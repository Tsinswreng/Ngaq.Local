namespace Ngaq.Local.Word.Dao;
using Ngaq.Core.Model.Po.Word;
using Tsinswreng.CsSqlHelper;
using Ngaq.Local.Db.TswG;
using Ngaq.Core.Shared.User.UserCtx;
using Ngaq.Core.Shared.Word.Models.Po.Word;

public partial class DaoSqlWord{
/// <summary>
/// 不校驗、直接update語句㕥改
/// 慎用 恐致謬。一般ʹ況ʸ直˪ 先軟刪舊詞後建新詞即可
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
		var UpdUpd = await FnTriggerOnRootAfterUpd(Ctx,Ct);
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
	.AddRaw(PId, T.UpperToRaw(IdWord))
	.AddRaw(PHead, Head)
	.AddRaw(PLang, Lang)
	;
	await SqlCmd.WithCtx(Ctx).Args(Arg).All(Ct);
	await UpdUpd(IdWord, Ct);
	return NIL;
};
	}
}
