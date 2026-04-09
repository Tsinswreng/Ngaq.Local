using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.Word.Models.Dto;
using Ngaq.Core.Shared.Word.Models.Po.UserLang;
using Ngaq.Core.Shared.Word.Models.Po.Word;
using Tsinswreng.CsPage;
using Tsinswreng.CsSql;

namespace Ngaq.Local.Domains.Word.Dao;

public class DaoUserLang(
	ITblMgr TblMgr
	,ISqlCmdMkr SqlCmdMkr
	,IRepo<PoUserLang, IdUserLang> RepoUserLang
){
	
	
	ITable<PoUserLang> T = TblMgr.GetTbl<PoUserLang>();
	ITable<PoWord> TW = TblMgr.GetTbl<PoWord>();

	/// 批量按語言唯一名查詢用戶語言。
	/// 輸入流只消費一次，且輸出與輸入位置對齊；不存在時返回 null。
	/// <param name="Ctx">數據庫函數上下文。</param>
	/// <param name="Owner">當前用戶 Id。</param>
	/// <param name="UniqNames">待查語言唯一名序列。</param>
	/// <param name="Ct">取消令牌。</param>
	/// <returns>對齊輸入順序的 <see cref="PoUserLang"/> 可空序列。</returns>
	public IAsyncEnumerable<PoUserLang?> BatGetUserLang(
		IDbFnCtx Ctx,
		IdUser Owner,
		IAsyncEnumerable<str> UniqNames,
		CT Ct
	){
		// 將 Owner 與 UniqName 綁成同一條輸入流，避免對可迭代參數的多次消費。
		var Owner_UniqName = UniqNames.Select(x=>(Owner, UniqName: x));
		var Sql = T.SqlSplicer().Select("*").From().WhereNonDel()
			.AndEq(x=>x.Owner, x=>x.Many(Owner_UniqName, x=>x.Owner))
			.AndEq(x=>x.UniqName, x=>x.Many(Owner_UniqName, x=>x.UniqName))
		;
		return SqlCmdMkr.RunDupliSql(Ctx, T, Sql, Ct);
	}
	
	public async Task<IPageAsyE<PoUserLang>> PageUserLang(
		IDbFnCtx Ctx, IdUser Owner, ReqPageUserLang Req, CT Ct
	){
		var Sql = T.SqlSplicer().Select("*").From().WhereNonDel()
		.AndEq(x=>x.Owner, x=>x.One(Owner));
		if(!string.IsNullOrEmpty(Req.UniqNameSearch)){
			Sql.And();
			Sql.Bool(x=>x.UniqName, "LIKE", x=>x.One("%"+Req.UniqNameSearch+"%"));//一個用戶不會定義很多很多語言 故可接受不經索引
		}
		Sql.OrderBy([
			T.QtCol(x=>x.BizUpdatedAt)+" Desc"
			,T.QtCol(x=>x.Id)+" Desc"
		])
		.LimOfst(Req.PageQry)
		;
		var r = SqlCmdMkr.RunDupliSql(Ctx, T, Sql, Ct);
		IAsyncEnumerable<PoUserLang> r2 = r.Where(x=>x is not null).Select(x=>x!);
		return Req.PageQry.ToPageAsyE(r2);
	}
	
	public async IAsyncEnumerable<str> GetUnregisteredUserLangs(
		IDbFnCtx Ctx, IdUser Owner, CT Ct
	){
		var POwner = T.Prm("Owner"); var NLang = nameof(PoWord.Lang);
		var Sql = 
$"""
SELECT DISTINCT w.{TW.QtCol(x=>x.Lang)} AS {NLang}
FROM {TW.Qt(TW.DbTblName)} w
WHERE 1=1
AND w.{TW.QtCol(x=>x.Owner)} = {POwner}
AND NOT EXISTS (
	SELECT 1
	FROM {T.Qt(T.DbTblName)} u
	WHERE u.{T.QtCol(x=>x.Owner)} = w.{TW.QtCol(x=>x.Owner)}
	AND u.{T.QtCol(x=>x.UniqName)} = w.{TW.QtCol(x=>x.Lang)}
);
""";
		var Cmd = await SqlCmdMkr.Prepare(Ctx, Sql, Ct);
		var rawDict = Cmd.Args(ArgDict.Mk(T).AddT(POwner, Owner)).AsyE1d(Ct);
		var gotLangs = rawDict.Select(x=>(str)x[NLang]);
		await foreach(var lang in gotLangs){
			yield return lang;
		}
	}
}
