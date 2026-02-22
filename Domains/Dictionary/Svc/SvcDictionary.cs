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


namespace Ngaq.Local.Domains.Dictionary.Svc;
using Kv = System.Collections.Generic.Dictionary<string, object?>;
using IKv = System.Collections.Generic.IDictionary<string, object?>;
public class SvcDictionary:ISvcDictionary{
	ICfgAccessor Cfg;
	IJsonSerializer Json;
	HttpClient HttpClient;
	IDictMapperShallow DictMapper;

	public SvcDictionary(
		ICfgAccessor Cfg
		,IJsonSerializer Json
		,IDictMapperShallow DictMapper
	){
		this.Cfg = Cfg;
		this.Json = Json;
		this.HttpClient = new HttpClient();
		this.DictMapper = DictMapper;
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
	public async Task<RespLlmDict> Lookup(IUserCtx User, ReqLlmDict Req, CT Ct){
		var apiUrl = Cfg.Get(ItemsClientCfg.LlmDictionary.ApiUrl);
		var apiKey = Cfg.Get(ItemsClientCfg.LlmDictionary.ApiKey);
		var model = Cfg.Get(ItemsClientCfg.LlmDictionary.Model);

		if(string.IsNullOrWhiteSpace(apiUrl) || string.IsNullOrWhiteSpace(apiKey)){
			throw ItemsErr.Dictionary.LlmApiNotConfigured.ToErr();
		}

		var prompt = BuildPrompt(Req);
		var responseText = await CallLlmApi(apiUrl, apiKey, model, prompt, Ct);
		var result = ParseResponse(responseText);
		return result;
	}

	private string BuildPrompt(ReqLlmDict Req){
		return $"{DfltPrompt.Prompt}\n\n---\n\n以下是用户的查询请求：\n\n{Json.Stringify(Req)}";
	}

//TODO 參數和返回值 都改成 DTO
	private async Task<string> CallLlmApi(string apiUrl, string apiKey, string model, string prompt, CT Ct){
		var requestBody = new Kv{
			["model"] = model,
			["messages"] = new List<obj?>{
				new Kv{
					["role"] = "user",
					["content"] = prompt
				}
			}
		};

		var json = ToolJson.DictToJson(requestBody);
		var content = new StringContent(json, Encoding.UTF8, "application/json");

		var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
		request.Content = content;

		var response = await HttpClient.SendAsync(request, Ct);
		response.EnsureSuccessStatusCode();

		var responseJson = await response.Content.ReadAsStringAsync(Ct);
		var llmResponse = ToolJson.JsonStrToDict(responseJson);
		if(llmResponse == null){
			throw ItemsErr.Dictionary.LlmApiEmptyResponse.ToErr();
		}

		// 使用 JsonNode 简化访问
		var node = new JsonNode(llmResponse);

		// 使用路径访问获取 content: choices[0].message.content
		if(!node.TryGetNodeByPath("choices[0].message.content", out var contentNode)){
			throw ItemsErr.Dictionary.LlmApiInvalidResponseStructure.ToErr();
		}

		var content_result = contentNode?.ValueObj?.ToString();
		if(string.IsNullOrEmpty(content_result)){
			throw ItemsErr.Dictionary.LlmApiEmptyContent.ToErr();
		}

		return content_result;
	}


	/// 解析 LLM 響應文本為 RespLlmDict
	/// TODO 參數和返回值都用DTO。
	private RespLlmDict ParseResponse(string LlmRespText, str RespContent){
		try{
			var textBlock = MdTextBlock.GetTextBlock(LlmRespText);
			var yamlMdText = "";
			if(textBlock == null){
				yamlMdText = LlmRespText;
			}else if(textBlock.Lang == "md" || textBlock.Lang == "markdown"){
				yamlMdText = textBlock.Text;
			}
			var yaml = Tsinswreng.CsYamlMd.YamlMd.Inst.ToYaml(yamlMdText);
			var dict = ToolYaml.YamlStrToDict(yaml);
			var R = new RespLlmDict();
			DictMapper.AssignShallowT(R, dict);
			return R;
		}catch(System.Exception ex){
			throw ItemsErr.Dictionary.LlmResponseParseFailed.ToErr().AddDebugArgs(ex, RespContent);
		}
	}
}
