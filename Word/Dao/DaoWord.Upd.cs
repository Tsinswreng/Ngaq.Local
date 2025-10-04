namespace Ngaq.Local.Word.Dao;
using Ngaq.Core.Infra;
using Ngaq.Core.Model.Po;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Local.Db;
using Tsinswreng.CsSqlHelper;
using Str_Any = System.Collections.Generic.Dictionary<str, obj?>;
using IStr_Any = System.Collections.Generic.IDictionary<str, obj?>;
using Ngaq.Core.Infra.Errors;
using Ngaq.Core.Models.Po;
using Ngaq.Core.Word.Models.Po.Learn;
using Tsinswreng.CsPage;
using Ngaq.Core.Word.Models;
using Ngaq.Core.Models.UserCtx;
using Tsinswreng.CsTools;
using Ngaq.Local.Db.TswG;
using Ngaq.Core.Model.Sys.Po.User;
using System.Diagnostics;
using Ngaq.Core.Word.Models.Dto;
using Ngaq.Core.Model.UserCtx;
using Ngaq.Core.Word.Models.Po.Word;
using Ngaq.Core.Word.Models.Po.Kv;

public partial class DaoSqlWord{
/// <summary>
/// 不校驗、直接update語句㕥改
/// </summary>
/// <param name="Ctx"></param>
/// <param name="Ct"></param>
/// <returns></returns>
	public async Task<Func<
		IUserCtx
		,IdWord
		,str//head
		,str//lang
		,CT
		,Task<nil>
	>> FnUpdPoWordHeadLang(IDbFnCtx Ctx, CT Ct){
var T = TblMgr.GetTbl<PoWord>();
var N = new PoWord.N();
var PId = T.Prm(N.Id); var PHead = T.Prm(N.Head); var PLang = T.Prm(N.Lang);
var Sql =
$"""
UPDATE {T.DbTblName}
SET {T.Fld(N.Head)} = {PHead}
,{T.Fld(N.Lang)} = {PLang}
WHERE {T.Fld(N.Id)} = {PId}
""";
var SqlCmd = await SqlCmdMkr.Prepare(Ctx, Sql, Ct);
Ctx?.AddToDispose(SqlCmd);
return async(UserCtx, IdWord, Head, Lang, Ct)=>{
	var Arg = ArgDict.Mk()
	.Add(PId, T.UpperToRaw(IdWord))
	.Add(PHead, Head)
	.Add(PLang, Lang)
	;
	await SqlCmd.WithCtx(Ctx).Args(Arg).All(Ct);
	return NIL;
};
	}
}
