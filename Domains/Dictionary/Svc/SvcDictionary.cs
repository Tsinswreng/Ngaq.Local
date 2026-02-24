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
using Tsinswreng.CsDictMapper;
using Tsinswreng.CsYamlMd;
using Microsoft.Extensions.Logging;



namespace Ngaq.Local.Domains.Dictionary.Svc;
using Kv = System.Collections.Generic.Dictionary<string, object?>;
using IKv = System.Collections.Generic.IDictionary<string, object?>;

#region DTOs

/// <summary>
/// DTO for LLM API request body
/// </summary>
public class DtoLlmApiReq{
	public string? Model{get;set;}
	public List<DtoLlmMessage>? Messages{get;set;}
}

/// <summary>
/// DTO for LLM message
/// </summary>
public class DtoLlmMessage{
	public string? Role{get;set;}
	public string? Content{get;set;}
}

/// <summary>
/// DTO for LLM API call parameters
/// </summary>
public class DtoLlmCallParam{
	public string? ApiUrl{get;set;}
	public string? ApiKey{get;set;}
	public string? Model{get;set;}
	public string? Prompt{get;set;}
}

/// <summary>
/// DTO for LLM API response
/// </summary>
public class DtoLlmApiResp{
	/// <summary>
	/// Raw JSON response text (for debugging)
	/// </summary>
	public string? RawResponse{get;set;}

	/// <summary>
	/// Extracted content from choices[0].message.content
	/// </summary>
	public string? Content{get;set;}
}

#endregion

public class SvcDictionary:ISvcDictionary{
	ICfgAccessor Cfg;
	IJsonSerializer JsonS;
	HttpClient HttpClient;
	IDictMapperShallow DictMapper;
	ILogger Logger;
	public SvcDictionary(
		ICfgAccessor Cfg
		,IJsonSerializer JsonS
		,IDictMapperShallow DictMapper
		,ILogger Logger
	){
		this.Cfg = Cfg;
		this.JsonS = JsonS;
		this.HttpClient = new HttpClient();
		this.DictMapper = DictMapper;
		this.Logger = Logger;
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

		var prompt = BuildPrompt(Req);

		var dtoParam = new DtoLlmCallParam{
			ApiUrl = apiUrl,
			ApiKey = apiKey,
			Model = model,
			Prompt = prompt
		};

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

		var result = await CallLlmApiStream(evtToUse, dtoParam, Ct);
		return result;
	}

	private string BuildPrompt(IReqLlmDict Req){
		return $"{DfltPrompt.Prompt}\n\n---\n\n以下是用户的查询请求：\n\n{JsonS.Stringify(Req)}";
	}

	/// <summary>
	/// Call LLM API and return DTO with raw response for debugging
	/// </summary>
	private async Task<DtoLlmApiResp> CallLlmApi(DtoLlmCallParam param, CT Ct){
		var requestBody = new DtoLlmApiReq{
			Model = param.Model,
			Messages = new List<DtoLlmMessage>{
				new DtoLlmMessage{
					Role = "user",
					Content = param.Prompt
				}
			}
		};

		var json = ToolJson.DictToJson(new Kv{
			["model"] = requestBody.Model,
			["messages"] = requestBody.Messages?.Select(m => new Kv{
				["role"] = m.Role,
				["content"] = m.Content
			}).ToList()
		});
		var content = new StringContent(json, Encoding.UTF8, "application/json");

		var request = new HttpRequestMessage(HttpMethod.Post, param.ApiUrl);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", param.ApiKey);
		request.Content = content;

		var response = await HttpClient.SendAsync(request, Ct);
		response.EnsureSuccessStatusCode();

		var responseJson = await response.Content.ReadAsStringAsync(Ct);
		Logger.LogInformation("LLM API raw response: {Response}", responseJson);

		var llmResponse = ToolJson.JsonStrToDict(responseJson);
		if(llmResponse == null){
			throw ItemsErr.Dictionary.LlmApiEmptyResponse.ToErr()
				.AddDebugArgs(responseJson);
		}

		// 使用 JsonNode 简化访问
		var node = new JsonNode(llmResponse);

		// 使用路径访问获取 content: choices[0].message.content
		if(!node.TryGetNodeByPath("choices[0].message.content", out var contentNode)){
			throw ItemsErr.Dictionary.LlmApiInvalidResponseStructure.ToErr()
				.AddDebugArgs(responseJson);
		}

		var content_result = contentNode?.ValueObj?.ToString();
		if(string.IsNullOrEmpty(content_result)){
			throw ItemsErr.Dictionary.LlmApiEmptyContent.ToErr()
				.AddDebugArgs(responseJson);
		}

		return new DtoLlmApiResp{
			RawResponse = responseJson,
			Content = content_result
		};
	}


	/// <summary>
	/// 调用 LLM API 流式输出
	/// </summary>
	private async Task<IRespLlmDict> CallLlmApiStream(IReqLlmDictEvt evt, DtoLlmCallParam param, CT Ct){
		var json = ToolJson.DictToJson(new Kv{
			["model"] = param.Model,
			["messages"] = new List<Kv>{
				new Kv{
					["role"] = "user",
					["content"] = param.Prompt
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

		// 构建最终响应
		var dtoResp = new DtoLlmApiResp{
			Content = fullContent.ToString()
		};

		return ParseResponse(dtoResp);
	}

	/// 解析 LLM 響應文本為 RespLlmDict
	private IRespLlmDict ParseResponse(DtoLlmApiResp dtoResp){
		var rawResponse = dtoResp.RawResponse;
		var content = dtoResp.Content;

		try{
			var textBlock = MdTextBlock.GetTextBlock(content);
			var yamlMdText = "";
			if(textBlock == null){
				throw ItemsErr.Dictionary.LlmResponseParseFailed.ToErr();
			}else if(textBlock.Lang == "md" || textBlock.Lang == "markdown"){
				yamlMdText = textBlock.Text;
			}else if(textBlock.Lang == "yaml" || textBlock.Lang == "yml"){
				yamlMdText = content;
			}
			var yaml = Tsinswreng.CsYamlMd.YamlMd.Inst.ToYaml(yamlMdText);
			var dict = ToolYaml.YamlStrToDict(yaml);
			var json = ToolJson.DictToJson(dict);
			var R = JsonS.Parse<RespLlmDict>(json);
			return R;
		}catch(System.Exception ex){
			Logger.LogError(ex, "Failed to parse LLM response. Raw response: {RawResponse}", rawResponse);
			throw ItemsErr.Dictionary.LlmResponseParseFailed.ToErr()
				.AddDebugArgs(ex, rawResponse);
		}
	}
}
