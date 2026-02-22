namespace Ngaq.Local.Domains.Word.Svc;
using Ngaq.Core.Shared.Base.Models.Req;
using Ngaq.Core.Shared.Base.Models.Resp;
using Ngaq.Core.Shared.User.UserCtx;
using Ngaq.Core.Shared.Word.Models.DictionaryApi;
using Ngaq.Core.Shared.Word.Models.Dto;
using Ngaq.Core.Shared.Word.Svc;
using Ngaq.Core.Tools.Json;
using Tsinswreng.CsTools;


[Obsolete]
public class SvcDictionary: ObsltISvcDictionary{
	IJsonSerializer JsonS;
	public SvcDictionary(
		IJsonSerializer JsonS
	){
		var z = this;
		z.JsonS = JsonS;
	}
	public async Task<RespLookup> LookupAsy(
		IUserCtx User
		,ReqLookup Req,CT Ct
	){
		//https://api.dictionaryapi.dev/api/v2/entries/en/hello
		var url = $"https://api.dictionaryapi.dev/api/v2/entries/en/{Uri.EscapeDataString(Req.SearchText)}";
		using var hc = new HttpClient();
		var json = await hc.GetStringAsync(url, Ct);
		//var dict = ToolJson.JsonStrToDict(json);
		try{
			var arr = JsonS.Parse<DictionaryApiWord[]>(json);
			return new RespLookup { DictionaryApiWords = arr };
		}
		catch(Exception e){
		}
		return new RespLookup();
	}
}
