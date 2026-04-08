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



namespace Ngaq.Local.Domains.Dictionary.Svc;
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
		var apiUrl = Cfg.Get(ItemsClientCfg.LlmDictionary.ApiUrl);
		var apiKey = Cfg.Get(ItemsClientCfg.LlmDictionary.ApiKey);
		var model = Cfg.Get(ItemsClientCfg.LlmDictionary.Model);

		if(string.IsNullOrWhiteSpace(apiUrl) || string.IsNullOrWhiteSpace(apiKey)){
			throw ItemsErr.Dictionary.LlmApiNotConfigured.ToErr();
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
			UserPrompt = userPrompt
		};

		var result = await CallLlmApiStream(evtToUse, dtoParam, Ct);
		return result;
	}

	private string BuildUserPrompt(IReqLlmDict Req){
		return JsonS.Stringify(Req);
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
		var json = ToolJson.DictToJson(new Kv{
			["model"] = param.Model,
			["messages"] = new List<IKv>{
				new Kv{
					["role"] = "system",
					["content"] = DfltPrompt.Prompt
				},
				new Kv{
					["role"] = "user",
					["content"] = param.UserPrompt
				}
			},
			["stream"] = true // 启用流式输出
		});
		var content = new StringContent(json, Encoding.UTF8, "application/json");

		var request = new HttpRequestMessage(HttpMethod.Post, param.ApiUrl);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", param.ApiKey);
		request.Content = content;

		// 使用 ResponseHeadersRead 以支持流式读取
		var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, Ct);
		response.EnsureSuccessStatusCode();

		var fullContent = new StringBuilder();
		try{
			using var stream = await response.Content.ReadAsStreamAsync(Ct);
			using var reader = new StreamReader(stream);

			// 逐行解析 SSE
			while(await reader.ReadLineAsync() is { } line){
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
			throw ItemsErr.Dictionary.LlmResponseParseFailed.ToErr()
				.AddDebugArgs(ex, rawResponse, content);
		};
	}
}
