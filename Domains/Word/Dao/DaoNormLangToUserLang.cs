using Ngaq.Core.Shared.Dictionary.Models;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.Word.Models.Dto;
using Ngaq.Core.Shared.Word.Models.Po.NormLangToUserLang;
using Tsinswreng.CsPage;
using Tsinswreng.CsSql;
using Tsinswreng.CsTools;

namespace Ngaq.Local.Domains.Word.Dao;

public class DaoNormLangToUserLang(
	ITblMgr TblMgr,
	ISqlCmdMkr SqlCmdMkr
){
	ITable<PoNormLangToUserLang> T = TblMgr.GetTbl<PoNormLangToUserLang>();

	public async Task<str?> GetUserLangByNormLang(
		IDbFnCtx Ctx,
		IdUser Owner,
		ELangIdentType NormLangType,
		str NormLang,
		CT Ct
	){
		var Sql = T.SqlSplicer().Select(x=>x.UserLang).From().WhereNonDel()
			.AndEq(x=>x.Owner, x=>x.One(Owner))
			.AndEq(x=>x.NormLangType, x=>x.One(NormLangType))
			.AndEq(x=>x.NormLang, x=>x.One(NormLang))
			.OrderBy([T.QtCol(x=>x.Id)+" Desc"])
			.Lim(1)
		;
		var Dicts = SqlCmdMkr.RunDupliSql(Ctx, Sql, Ct);
		var UserLang = Dicts.Select(x=>x is null ? null : (str?)x[T.Memb(y=>y.UserLang)]);
		return await UserLang.FirstOrDefaultAsync(Ct);
	}

	public async Task<IPageAsyE<PoNormLangToUserLang>> PageNormLangToUserLang(
		IDbFnCtx Ctx,
		IdUser Owner,
		ReqPageNormLangToUserLang Req,
		CT Ct
	){
		var Sql = T.SqlSplicer().Select("*").From().WhereNonDel()
			.AndEq(x=>x.Owner, x=>x.One(Owner))
		;
		if(!string.IsNullOrEmpty(Req.UserLang)){
			Sql.And();
			Sql.Bool(x=>x.UserLang, "LIKE", x=>x.One("%"+Req.UserLang+"%"));
		}
		Sql.OrderBy([
			T.QtCol(x=>x.BizUpdatedAt)+" Desc",
			T.QtCol(x=>x.Id)+" Desc"
		])
		.LimOfst(Req.PageQry)
		;
		var R = SqlCmdMkr.RunDupliSql(Ctx, T, Sql, Ct);
		IAsyncEnumerable<PoNormLangToUserLang> R2 = R.Where(x=>x is not null).Select(x=>x!);
		return Req.PageQry.ToPageAsyE(R2);
	}
}
