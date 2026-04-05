using Ngaq.Core.Infra;
using Ngaq.Core.Shared.Word.Models.Dto;
using Ngaq.Core.Shared.Word.Models.Po.UserLang;
using Ngaq.Core.Shared.Word.Svc;
using Ngaq.Local.Domains.Word.Dao;
using Tsinswreng.CsPage;
using Tsinswreng.CsSql;

namespace Ngaq.Local.Domains.Word.Svc;
public class SvcUserLang(
	DaoUserLang Dao
)
	:ISvcUserLang
{
	public async Task<IPageAsyE<PoUserLang>> PageUserLang(
		IDbUserCtx Ctx, ReqPageUserLang Req, CT Ct
	){
		Ctx.DbFnCtx??= new DbFnCtx();
		return await Dao.PageUserLang(Ctx.DbFnCtx, Ctx.UserCtx.UserId, Req, Ct);
	}
}
