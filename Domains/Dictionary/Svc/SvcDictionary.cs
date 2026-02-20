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

namespace Ngaq.Local.Domains.Dictionary.Svc;

public class SvcDictionary:ISvcDictionary{
	ICfgAccessor Cfg;
	IJsonSerializer Json;
	HttpClient HttpClient;

	public SvcDictionary(
		ICfgAccessor Cfg,
		IJsonSerializer Json
	){
		this.Cfg = Cfg;
		this.Json = Json;
		this.HttpClient = new HttpClient();
	}

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

		var deserializer = new DeserializerBuilder()
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.Build();

		var yamlObj = deserializer.Deserialize<Dictionary<string, object>>(yaml);

		var result = new RespLlmDict();

		if(yamlObj.TryGetValue("Head", out var head)){
			result.Head = head?.ToString() ?? "";
		}

		if(yamlObj.TryGetValue("Pronunciations", out var prons)){
			if(prons is IList<object> pronList){
				foreach(var pron in pronList){
					result.Pronunciations.Add(new Ngaq.Core.Shared.Word.Models.TextedPronunciation{
						Text = pron?.ToString() ?? ""
					});
				}
			}
		}

		if(yamlObj.TryGetValue("Descrs", out var descrs)){
			if(descrs is IList<object> descrList){
				foreach(var descr in descrList){
					result.Descrs.Add(descr?.ToString() ?? "");
				}
			}
		}

		return result;
	}
}
