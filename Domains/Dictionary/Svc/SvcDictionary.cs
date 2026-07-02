using Ngaq.Core.Shared.Dictionary.Models;
using Ngaq.Core.Shared.Dictionary.Svc;
using Ngaq.Core.Shared.User.UserCtx;
using Ngaq.Core.Infra.Cfg;
using Ngaq.Core.Tools.Json;
using Ngaq.Core.Infra.Errors;
using Tsinswreng.CsCfg;
using Tsinswreng.CsErr;
using Tsinswreng.CsCore;
using Tsinswreng.CsTools;
using System.Net.Http.Headers;
using System.Text;
using Ngaq.Core.Tools;
using Tsinswreng.CsYamlMd;
using Microsoft.Extensions.Logging;
using Ngaq.Core.Infra;
using Ngaq.Core.Shared.Dictionary.Models.Po.NormLang;
using Ngaq.Core.Shared.Kv;
using Ngaq.Core.Shared.Kv.Models;
using Ngaq.Core.Shared.Kv.Svc;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Shared.Word.Svc;
using Tsinswreng.CsSql;



namespace Ngaq.Backend.Domains.Dictionary.Svc;
using Kv = System.Collections.Generic.Dictionary<string, object?>;
using IKv = System.Collections.Generic.IDictionary<string, object?>;

#region DTOs

/// DTO for LLM API request body
public class DtoLlmApiReq{
	public string? Model{get;set;}
	public List<DtoLlmMessage>? Messages{get;set;}
}

/// DTO for LLM message
public class DtoLlmMessage{
	public string? Role{get;set;}
	public string? Content{get;set;}
}

/// DTO for LLM API call parameters
public class DtoLlmCallParam{
	public string? ApiUrl{get;set;}
	public string? ApiKey{get;set;}
	public string? Model{get;set;}
	/// 額外透傳到請求體頂層的 provider-specific JSON 對象。
	public string? ExtraBodyJson{get;set;}

	/// 用戶提示詞

	public string? UserPrompt{get;set;}
}

/// DTO for LLM API response
public class DtoLlmApiResp{

	/// Raw JSON response text (for debugging)

	public string? RawResponse{get;set;}


	/// Extracted content from choices[0].message.content

	public string? Content{get;set;}
}

#endregion

public class SvcDictionary:ISvcDictionary{
	ICfgAccessor Cfg;
	IJsonSerializer JsonS;
	HttpClient HttpClient;
	ILogger Logger;
	ISvcKv SvcKv;
	ISvcNormLang SvcNormLang;
	IRepo<PoKv, IdKv> RepoKv;
	ISqlCmdMkr SqlCmdMkr;
	public SvcDictionary(
		ICfgAccessor Cfg
		,IJsonSerializer JsonS
		,ILogger Logger
		,ISvcKv SvcKv
		,ISvcNormLang SvcNormLang
		,IRepo<PoKv, IdKv> RepoKv
		,ISqlCmdMkr SqlCmdMkr
	){
		this.Cfg = Cfg;
		this.JsonS = JsonS;
		this.HttpClient = new HttpClient();
		this.Logger = Logger;
		this.SvcKv = SvcKv;
		this.SvcNormLang = SvcNormLang;
		this.RepoKv = RepoKv;
		this.SqlCmdMkr = SqlCmdMkr;
	}

	public async Task<PoNormLang?> GetCurSrcNormLang(IDbUserCtx Ctx, CT Ct){
		return await LoadOrInitCurLang(
			Ctx, KeysKv.Dictionary.CurSrcLang+"", "en", "English", Ct
		);
	}

	public async Task<PoNormLang?> SetCurSrcNormLang(
		IDbUserCtx Ctx, PoNormLang Po, CT Ct
	){
		var normalized = NormalizeCurLang(Ctx, Po);
		await SaveCurLang(Ctx, KeysKv.Dictionary.CurSrcLang+"", normalized, Ct);
		return normalized;
	}

	public async Task<PoNormLang?> GetCurTgtNormLang(IDbUserCtx Ctx, CT Ct){
		return await LoadOrInitCurLang(
			Ctx, KeysKv.Dictionary.CurTgtLang+"", "zh", "中文", Ct
		);
	}

	public async Task<PoNormLang?> SetCurTgtNormLang(
		IDbUserCtx Ctx, PoNormLang Po, CT Ct
	){
		var normalized = NormalizeCurLang(Ctx, Po);
		await SaveCurLang(Ctx, KeysKv.Dictionary.CurTgtLang+"", normalized, Ct);
		return normalized;
	}

	/// 讀取詞典系統提示語。
	/// 若配置中尚未設置，則把默認提示語寫回配置後返回，保證設置頁能看到可編輯的初始值。
	public async Task<str> GetLlmDictSysPromptOrDflt(IDbUserCtx Ctx, CT Ct){
		_ = Ctx;
		var Prompt = Cfg.Get(KeysClientCfg.LlmDictionary.Prompt);
		if(!str.IsNullOrWhiteSpace(Prompt)){
			return Prompt!;
		}
		var Dflt = DfltPrompt.Prompt;
		Cfg.Set(KeysClientCfg.LlmDictionary.Prompt, Dflt);
		await Cfg.Save(Ct);
		return Dflt;
	}

	/// 保存詞典系統提示語，並返回保存後的值。
	public async Task<str> SetLlmDictSysPrompt(IDbUserCtx Ctx, str Prompt, CT Ct){
		_ = Ctx;
		var Normalized = string.IsNullOrWhiteSpace(Prompt) ? DfltPrompt.Prompt : Prompt;
		Cfg.Set(KeysClientCfg.LlmDictionary.Prompt, Normalized);
		await Cfg.Save(Ct);
		return Normalized;
	}

	public Task<IList<NormLang>> GetRecentUsedNormLangs(IDbUserCtx Ctx, CT Ct){
		IList<NormLang> r = [];
		return Task.FromResult(r);
	}

	/*
如果AI響應的文本中把YamlMd格式又包進代碼塊的話、你要先去掉最外層的代碼塊
具體的判斷方法:
去掉開頭的空白字符
如果響應文本中是以 ```yaml 開頭 就是正確的格式
如果以```md 或 ````md 或 `````md (或者有更多的反點)、就要先把這層代碼塊去掉
注意代碼塊起始界和終止界的反點的數量是一致的
*/
	[Impl]
	public async Task<IRespLlmDict> Lookup(IUserCtx User, IReqLlmDict Req, CT Ct){
		_ = User;
		var apiUrl = Cfg.Get(KeysClientCfg.LlmDictionary.ApiUrl);
		var apiKey = Cfg.Get(KeysClientCfg.LlmDictionary.ApiKey);
		var model = Cfg.Get(KeysClientCfg.LlmDictionary.Model);

		if(string.IsNullOrWhiteSpace(apiUrl) || string.IsNullOrWhiteSpace(apiKey)){
			throw KeysErr.Dictionary.LlmApiNotConfigured.ToErr();
		}

		var userPrompt = BuildUserPrompt(Req);

		// 统一走流式输出流程
		// 如果传参方不按流式输出传，就自己攒着等完了一起发结果回去
		ReqLlmDictEvt? internalEvt = null;
		IReqLlmDictEvt evtToUse;

		if(Req is IReqLlmDictEvt reqEvt){
			evtToUse = reqEvt;
		}else{
			// 创建内部事件对象来收集流式输出
			internalEvt = new ReqLlmDictEvt();
			evtToUse = internalEvt;
		}

		var dtoParam = new DtoLlmCallParam{
			ApiUrl = apiUrl,
			ApiKey = apiKey,
			Model = model,
			ExtraBodyJson = Cfg.Get(KeysClientCfg.LlmDictionary.ExtraBodyJson),
			UserPrompt = userPrompt
		};

		var result = await CallLlmApiStream(evtToUse, dtoParam, Ct);
		return result;
	}

	/// 構造發給大模型的用戶提示詞。
	/// 此處只負責傳遞本次查詞的請求信息；格式與約束由 system prompt 負責。
	private string BuildUserPrompt(IReqLlmDict Req){
		var R =
$"""
the word that user wants to search:
{Req.Query.Term}

ContextSentence:
{Req.Query.ContextSentence}

- SourceLanguage(the language of the input text):
{FormatLang(Req.OptLang.SrcLang)}

- TargetLanguages(the language(s) that user wants to get the definition in):
{FormatLangList(Req.OptLang.TgtLangs)}

Preferences:
- {FormatPreferences(Req.Preferences)}
""";
		return R;
	}

	/// 直接解析 LLM 原始輸出文本，不觸發外部 API 調用。
	public IRespLlmDict ParseRawOutput(str RawOutput){
		var dtoResp = new DtoLlmApiResp{
			RawResponse = RawOutput,
			Content = RawOutput,
		};
		return ParseResponse(dtoResp);
	}

	private async Task<PoNormLang?> LoadOrInitCurLang(
		IDbUserCtx Ctx,
		str Key,
		str DfltCode,
		str DfltNativeName,
		CT Ct
	){
		Ctx.DbFnCtx ??= new DbFnCtx();
		var owner = Ctx.UserCtx.UserId;
		var kv = await SvcKv.BatGetByOwnerEtKStr(
			Ctx.DbFnCtx,
			ToolAsyE.ToAsyE([(owner, Key)]),
			Ct
		).FirstOrDefaultAsync(Ct);

		var parsed = ParsePoNormLang(kv?.GetVStr());
		if(parsed is not null && !str.IsNullOrWhiteSpace(parsed.Code)){
			parsed.Owner = owner;
			return parsed;
		}

		var dflt = await GetOrMkBuiltinNormLang(Ctx, DfltCode, DfltNativeName, Ct);
		await SaveCurLang(Ctx, Key, dflt, Ct);
		return dflt;
	}

	private async Task<PoNormLang> GetOrMkBuiltinNormLang(
		IDbUserCtx Ctx,
		str Code,
		str DfltNativeName,
		CT Ct
	){
		var po = await SvcNormLang.BatGetNormLangByTypeCode(
			Ctx,
			ToolAsyE.ToAsyE([(ELangIdentType.Bcp47, Code)]),
			Ct
		).FirstOrDefaultAsync(Ct);
		if(po is not null){
			po.Owner = Ctx.UserCtx.UserId;
			return po;
		}
		return new PoNormLang{
			Owner = Ctx.UserCtx.UserId,
			Type = ELangIdentType.Bcp47,
			Code = Code,
			NativeName = DfltNativeName,
		};
	}

	private async Task<nil> SaveCurLang(
		IDbUserCtx Ctx,
		str Key,
		PoNormLang Po,
		CT Ct
	){
		Ctx.DbFnCtx ??= new DbFnCtx();
		var owner = Ctx.UserCtx.UserId;
		var json = JsonS.Stringify(Po);

		return await SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async DbCtx=>{
			var oldKv = await SvcKv.BatGetByOwnerEtKStr(
				DbCtx,
				ToolAsyE.ToAsyE([(owner, Key)]),
				Ct
			).FirstOrDefaultAsync(Ct);
			if(oldKv is null){
				var kv = new PoKv{
					Owner = owner,
				}.SetStrStr(Key, json);
				await RepoKv.BatAdd(DbCtx, ToolAsyE.ToAsyE([kv]), Ct);
				return NIL;
			}
			oldKv.Owner = owner;
			oldKv.SetStrStr(Key, json);
			await RepoKv.BatUpd(DbCtx, ToolAsyE.ToAsyE([oldKv]), Ct);
			return NIL;
		});
	}

	private static PoNormLang NormalizeCurLang(IDbUserCtx Ctx, PoNormLang Po){
		return new PoNormLang{
			Id = Po.Id,
			Owner = Ctx.UserCtx.UserId,
			Type = Po.Type == ELangIdentType.Unknown ? ELangIdentType.Bcp47 : Po.Type,
			Code = (Po.Code ?? "").Trim(),
			NativeName = (Po.NativeName ?? "").Trim(),
		};
	}

	/// 轉換單個語言描述，盡量把 code / native name / english name 都寫進 prompt。
	static str FormatLang(INormLangDetail Lang){
		var Segs = new List<str>{
			$"code={ToPromptValue(Lang.Code)}",
			$"type={Lang.Type}",
		};
		if(!str.IsNullOrWhiteSpace(Lang.NativeName)){
			Segs.Add($"NativeName={ToPromptValue(Lang.NativeName)}");
		}
		if(!str.IsNullOrWhiteSpace(Lang.EnglishName)){
			Segs.Add($"EnglishName={ToPromptValue(Lang.EnglishName)}");
		}
		return string.Join(", ", Segs);
	}

	/// 轉換多個目標語言描述。
	static str FormatLangList(IEnumerable<INormLangDetail> Langs){
		var Items = Langs.Select(FormatLang).Where(x=>!str.IsNullOrWhiteSpace(x));
		var Joined = string.Join("; ", Items);
		return Joined == "" ? "(none)" : Joined;
	}

	/// 將偏好配置轉為可讀文字；空值時顯式標識未指定，避免模型自行腦補。
	static str FormatPreferences(Preferences? Preferences){
		if(Preferences is null){
			return "";
		}
		return string.Join(", ", [
			$"TryIncludeExamples={Preferences.TryIncludeExamples}",
			$"TryIncludeSynonyms={Preferences.TryIncludeSynonyms}",
			$"TryIncludeAntonyms={Preferences.TryIncludeAntonyms}",
			$"TryIncludeEtymology={Preferences.TryIncludeEtymology}",
			$"MaxExamples={Preferences.MaxExamples}",
		]);
	}

	/// 將 prompt 值規整成單行文本；空白值統一標記為 null。
	static str ToPromptValue(str? Value){
		if(str.IsNullOrWhiteSpace(Value)){
			return "null";
		}
		return Value.Trim().Replace("\r", " ").Replace("\n", " ");
	}

	private PoNormLang? ParsePoNormLang(str? Json){
		if(str.IsNullOrWhiteSpace(Json)){
			return null;
		}
		try{
			return JsonS.Parse<PoNormLang>(Json!);
		}catch{
			return null;
		}
	}

	/// 调用 LLM API 流式输出
	private async Task<IRespLlmDict> CallLlmApiStream(
		IReqLlmDictEvt evt,
		DtoLlmCallParam param,
		CT Ct
	){
		var SysPrompt = Cfg.Get(KeysClientCfg.LlmDictionary.Prompt);
		if(string.IsNullOrWhiteSpace(SysPrompt)){
			SysPrompt = DfltPrompt.Prompt;
		}
		var reqBody = new Kv{
			["model"] = param.Model,
			["messages"] = new List<IKv>{
				new Kv{
					["role"] = "system",
					["content"] = SysPrompt
				},
				new Kv{
					["role"] = "user",
					["content"] = param.UserPrompt
				}
			},
			["stream"] = true // 启用流式输出
		};
		// 先構造通用 OpenAI-compatible 主體，再允許配置覆蓋/追加頂層字段，
		// 以便適配 DeepSeek 等 provider 的私有參數。
		MergeExtraBodyJson(reqBody, param.ExtraBodyJson);
		var json = ToolJson.DictToJson(reqBody);
		var content = new StringContent(json, Encoding.UTF8, "application/json");

		var request = new HttpRequestMessage(HttpMethod.Post, param.ApiUrl);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", param.ApiKey);
		request.Content = content;

		// 使用 ResponseHeadersRead 以支持流式读取
		using var response = await HttpClient
			.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, Ct)
			.ConfigureAwait(false);
		response.EnsureSuccessStatusCode();

		var fullContent = new StringBuilder();
		try{
			using var stream = await response.Content.ReadAsStreamAsync(Ct).ConfigureAwait(false);
			using var reader = new StreamReader(stream);

			// 逐行解析 SSE
			while(await reader.ReadLineAsync().ConfigureAwait(false) is { } line){
				if(string.IsNullOrEmpty(line)) continue;
				if(!line.StartsWith("data: ")) continue;

				var payload = line["data: ".Length..];
				if(payload == "[DONE]"){
					// 流结束
					evt.OnDone?.Invoke(new DtoOnDone(), Ct);
					break;
				}

				try{
					using var doc = System.Text.Json.JsonDocument.Parse(payload);
					var root = doc.RootElement;

					if(root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0){
						var delta = choices[0].GetProperty("delta");
						if(delta.TryGetProperty("content", out var contentProp)){
							var seg = contentProp.GetString();
							if(!string.IsNullOrEmpty(seg)){
								fullContent.Append(seg);
								// 触发事件
								evt.OnNewSeg?.Invoke(new DtoOnNewSeg{ NewSeg = seg }, Ct);
							}
						}
					}
				}catch(System.Text.Json.JsonException){
					// 忽略解析错误，继续处理下一行
					Logger.LogWarning("Failed to parse SSE line: {Line}", line);
				}
			}

			// 構建最終響應：RawResponse / Content 都記錄完整拼接文本，便於錯誤排查。
			var merged = fullContent.ToString();
			return ParseRawOutput(merged);
		}catch(Exception ex){
			Logger.LogError(
				ex,
				"LLM dictionary stream failed. Partial LLM response: {LlmResponse}",
				fullContent.ToString()
			);
			throw;
		}
	}

	/// 把配置中的頂層 JSON 對象 merge 到請求體。
	/// 當存在同名鍵時，採用配置值覆蓋默認值，方便顯式關閉 provider 默認行為。
	private static void MergeExtraBodyJson(Kv ReqBody, str? ExtraBodyJson){
		if(str.IsNullOrWhiteSpace(ExtraBodyJson)){
			return;
		}

		try{
			var extraDict = ToolJson.JsonStrToDict(ExtraBodyJson);
			if(extraDict is null){
				throw KeysErr.Common.ArgErr.ToErr()
					.AddDebugArgs(nameof(KeysClientCfg.LlmDictionary.ExtraBodyJson), ExtraBodyJson);
			}
			foreach(var kv in extraDict){
				ReqBody[kv.Key] = kv.Value;
			}
		}catch(AppErr){
			throw;
		}catch(Exception ex){
			throw KeysErr.Common.ArgErr.ToErr()
				.AddErr(ex)
				.AddDebugArgs(nameof(KeysClientCfg.LlmDictionary.ExtraBodyJson), ExtraBodyJson);
		}
	}

	/// 解析 LLM 響應文本為 RespLlmDict
	private IRespLlmDict ParseResponse(DtoLlmApiResp dtoResp){
		var rawResponse = dtoResp.RawResponse;
		var content = dtoResp.Content;
		try{
			var parseResult = YamlMd.Inst.TryToYamlLenient(content, out var yaml);
			if(parseResult == ELenientParseResult.Failed){
				Err(null);
			}
			var dict = ToolYaml.YamlStrToDict(yaml);
			var json = ToolJson.DictToJson(dict);
			var R = JsonS.Parse<RespLlmDict>(json);
			return R!;
		}catch(System.Exception ex){
			Err(ex);
		}
		return null!;
		
		void Err(Exception? ex){
			Logger.LogError(
				ex,
				"Failed to parse LLM response. Content: {Content}; Raw response: {RawResponse}",
				content,
				rawResponse
			);
			throw KeysErr.Dictionary.LlmResponseParseFailed.ToErr()
				.AddDebugArgs(ex, rawResponse, content);
		};
	}
}
