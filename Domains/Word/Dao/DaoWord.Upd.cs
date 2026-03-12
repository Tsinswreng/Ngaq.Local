namespace Ngaq.Local.Word.Dao;

using Tsinswreng.CsSql;
using Ngaq.Local.Db.TswG;
using Ngaq.Core.Shared.User.UserCtx;
using Ngaq.Core.Shared.Word.Models.Po.Word;

public partial class DaoWord{
/// 不校驗、直接update語句㕥改
/// 慎用 恐致謬。一般ʹ況ʸ直˪ 先軟刪舊詞後建新詞即可
	public async Task<Func<
		IUserCtx
		,IdWord
		,str//head
		,str//lang
		,CT
		,Task<nil>
	>> FnUpdPoWordHeadLang(IDbFnCtx Ctx, CT Ct){
		var UpdUpd = await FnTriggerOnRootAfterUpd(Ctx,Ct);

var Sql = T.SqlSplicer().UpdateSet()
.Eq(x=>x.Head, out var PHead)
.C().Eq(x=>x.Lang, out var PLang)
.Where1().AndEq(x=>x.Id, out var PId).ToSqlStr();
;

var SqlCmd = await SqlCmdMkr.Prepare(Ctx, Sql, Ct);
Ctx?.AddToDispose(SqlCmd);
return async(UserCtx, IdWord, Head, Lang, Ct)=>{
	var Arg = ArgDict.Mk()
	.AddRaw(PId, T.UpperToRaw(IdWord))
	.AddRaw(PHead, Head)
	.AddRaw(PLang, Lang)
	;
	await SqlCmd.Args(Arg).All1d(Ct);
	await UpdUpd(IdWord, Ct);
	return NIL;
};
	}
}
