using Ngaq.Core.Shared.Dictionary.Models;
using Ngaq.Core.Shared.Dictionary.Svc;
using Ngaq.Core.Shared.User.UserCtx;
using Ngaq.Core.Infra.Cfg;
using Ngaq.Core.Tools.Json;
using Tsinswreng.CsCfg;
using Tsinswreng.CsCore;
using Tsinswreng.CsTools;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Ngaq.Core.Tools;
using Tsinswreng.CsDictMapper;

namespace Ngaq.Local.Domains.Dictionary.Svc;

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
如果AI響應的文本中 把YamlMd格式又包進代碼塊的話、你要先去掉最外層的代碼塊
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
			throw new InvalidOperationException("LLM Dictionary API URL or API Key is not configured.");
		}

		var prompt = BuildPrompt(Req);
		var responseText = await CallLlmApi(apiUrl, apiKey, model, prompt, Ct);
		var result = ParseResponse(responseText);
		return result;
	}

	private string BuildPrompt(ReqLlmDict Req){
		return $"{DfltPrompt.Prompt}\n\n---\n\n以下是用户的查询请求：\n\n{Json.Stringify(Req)}";
	}

	private async Task<string> CallLlmApi(string apiUrl, string apiKey, string model, string prompt, CT Ct){
		var requestBody = new Dictionary<str, obj?>{
			["model"] = model,
			["messages"] = new List<obj?>{
				new Dictionary<str, obj?>{
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

		if(llmResponse == null || !llmResponse.TryGetValue("choices", out var choicesObj) || choicesObj is not IList<obj?> choices || choices.Count == 0){
			throw new InvalidOperationException("LLM API returned empty response.");
		}

		var firstChoice = choices[0] as IDictionary<str, obj?>;
		if(firstChoice == null || !firstChoice.TryGetValue("message", out var messageObj) || messageObj is not IDictionary<str, obj?> message){
			throw new InvalidOperationException("LLM API returned invalid response structure.");
		}

		var content_result = message.TryGetValue("content", out var contentObj) ? contentObj?.ToString() : null;
		if(string.IsNullOrEmpty(content_result)){
			throw new InvalidOperationException("LLM API returned empty content.");
		}

		return content_result;
	}

	private RespLlmDict ParseResponse(string yamlMdText){
		var yaml = Tsinswreng.CsYamlMd.YamlMd.Inst.ToYaml(yamlMdText);
		var dict = ToolYaml.YamlStrToDict(yaml);
		var R = new RespLlmDict();
		DictMapper.AssignShallowT(R, dict);
		return R;
	}
}
