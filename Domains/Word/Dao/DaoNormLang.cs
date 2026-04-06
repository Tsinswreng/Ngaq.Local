using Ngaq.Core.Shared.Dictionary.Models;
using Ngaq.Core.Shared.Dictionary.Models.Po.NormLang;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.Word.Models.Dto;
using Tsinswreng.CsPage;
using Tsinswreng.CsSql;

namespace Ngaq.Local.Domains.Word.Dao;

public class DaoNormLang(
	ITblMgr TblMgr,
	ISqlCmdMkr SqlCmdMkr
){
	ITable<PoNormLang> T = TblMgr.GetTbl<PoNormLang>();

	public IAsyncEnumerable<PoNormLang?> BatGetNormLangByTypeCode(
		IDbFnCtx Ctx,
		IdUser Owner,
		IAsyncEnumerable<(ELangIdentType, str)> Type_Code,
		CT Ct
	){
		var Types = Type_Code.Select(x=>x.Item1);
		var Codes = Type_Code.Select(x=>x.Item2);
		var Sql = T.SqlSplicer().Select("*").From().WhereNonDel()
			.AndEq(x=>x.Owner, x=>x.One(Owner))
			.AndEq(x=>x.Type, x=>x.Many(Types))
			.AndEq(x=>x.Code, x=>x.Many(Codes))
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
		if(!string.IsNullOrEmpty(Req.Code)){
			Sql.And();
			Sql.Bool(x=>x.Code, "LIKE", x=>x.One("%"+Req.Code+"%"));
		}
		Sql.OrderBy([
			T.QtCol(x=>x.BizUpdatedAt)+" Desc",
			T.QtCol(x=>x.Id)+" Desc"
		])
		.LimOfst(Req.PageQry)
		;
		var R = SqlCmdMkr.RunDupliSql(Ctx, T, Sql, Ct);
		IAsyncEnumerable<PoNormLang> R2 = R.Where(x=>x is not null).Select(x=>x!);
		return Req.PageQry.ToPageAsyE(R2);
	}
}
