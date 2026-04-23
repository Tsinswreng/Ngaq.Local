using Ngaq.Core.Shared.Dictionary.Models;
using Ngaq.Core.Shared.Dictionary.Models.Po.NormLang;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.Word.Models.Dto;
using Tsinswreng.CsPage;
using Tsinswreng.CsSql;

namespace Ngaq.Backend.Domains.Word.Dao;

public class DaoNormLang(
	ITblMgr TblMgr,
	ISqlCmdMkr SqlCmdMkr
){
	ITable<PoNormLang> T = TblMgr.GetTbl<PoNormLang>();

	public IAsyncEnumerable<PoNormLang?> BatGetNormLangByTypeCode(
		IDbFnCtx Ctx,
		IdUser Owner,
		IAsyncEnumerable<(ELangIdentType Type, str Code)> Type_Code,
		CT Ct
	){
		var tc = Type_Code;
		var Sql = T.SqlSplicer().Select("*").From().WhereNonDel()
			.AndEq(x=>x.Owner, x=>x.One(Owner))
			.AndEq(x=>x.Type, x=>x.Many(tc, x=>x.Type))
			.AndEq(x=>x.Code, x=>x.Many(tc, x=>x.Code))
		;
		return SqlCmdMkr.RunDupliSql(Ctx, T, Sql, Ct);
	}

	public async Task<IPageAsyE<PoNormLang>> PageNormLang(
		IDbFnCtx Ctx,
		IdUser Owner,
		ReqPageNormLang Req,
		CT Ct
	){
		var Sql = T.SqlSplicer().Select("*").From().WhereNonDel()
			.AndEq(x=>x.Owner, x=>x.One(Owner))
		;
		var SearchText = (Req.SearchText ?? "").Trim();
		if(!string.IsNullOrEmpty(SearchText)){
			var Like = "%"+SearchText+"%";
			Sql.And();
			Sql.Bool(x=>x.Code, "LIKE", x=>x.One(Like))
				.Or()
				.Bool(x=>x.NativeName, "LIKE", x=>x.One(Like));
		}
		Sql.OrderBy([
			T.QtCol(x=>x.Weight)+" Desc",
			T.QtCol(x=>x.Code)+" ",
		])
		.LimOfst(Req.PageQry)
		;
		var R = SqlCmdMkr.RunDupliSql(Ctx, T, Sql, Ct);
		IAsyncEnumerable<PoNormLang> R2 = R.Where(x=>x is not null).Select(x=>x!);
		return Req.PageQry.ToPageAsyE(R2);
	}
}
