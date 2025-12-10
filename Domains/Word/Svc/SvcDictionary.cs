namespace Ngaq.Local.Domains.Word.Svc;
using Ngaq.Core.Shared.Base.Models.Req;
using Ngaq.Core.Shared.Base.Models.Resp;
using Ngaq.Core.Shared.User.UserCtx;
using Ngaq.Core.Shared.Word.Models.DictionaryApi;
using Ngaq.Core.Tools.Json;
using Tsinswreng.CsTools;

public class ReqLookup:IReq{
	public str SearchText{get;set;}
}

public class RespLookup:IResp{
	public IList<DictionaryApiWord> DictionaryApiWords{get;set;}
}


public class SvcDictionary{
	IJsonSerializer JsonS;
	public SvcDictionary(
		IJsonSerializer JsonS
	){
		var z = this;
		z.JsonS = JsonS;
	}
	public async Task<RespLookup> Lookup(
		IUserCtx User
		,ReqLookup Req
	){
		//https://api.dictionaryapi.dev/api/v2/entries/en/hello
		// var url = $"https://api.dictionaryapi.dev/api/v2/entries/en/{Uri.EscapeDataString(Req.SearchText)}";
		// using var hc = new HttpClient();
		// var json = await hc.GetStringAsync(url);
		// //var dict = ToolJson.JsonStrToDict(json);
		// try{
		// 	var arr = JsonS.Parse<DictionaryApiWord[]>(json);
		// 	return new RespLookup { DictionaryApiWords = arr };
		// }
		// catch(){

		// }
		// return new RespLookup();
		throw new NotImplementedException();
	}
}
