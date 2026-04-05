using Ngaq.Core.Infra;
using Ngaq.Core.Infra.Errors;
using Ngaq.Core.Shared.Base.Models.Po;
using Ngaq.Core.Shared.Dictionary.Models;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.Word.Models.Dto;
using Ngaq.Core.Shared.Word.Models.Po.UserLang;
using Ngaq.Core.Shared.Word.Svc;
using Ngaq.Local.Db.TswG;
using Ngaq.Local.Domains.Word.Dao;
using Tsinswreng.CsErr;
using Tsinswreng.CsPage;
using Tsinswreng.CsSql;
using Tsinswreng.CsTools;

namespace Ngaq.Local.Domains.Word.Svc;
public class SvcUserLang(
	DaoUserLang Dao
	,ISqlCmdMkr SqlCmdMkr
	,IRepo<PoUserLang, IdUserLang> RepoUserLang
)
	:ISvcUserLang
{
	DaoUserLang Dao = Dao;
	ISqlCmdMkr SqlCmdMkr = SqlCmdMkr;
	IRepo<PoUserLang, IdUserLang> RepoUserLang = RepoUserLang;

	public async Task<IPageAsyE<PoUserLang>> PageUserLang(
		IDbUserCtx Ctx, ReqPageUserLang Req, CT Ct
	){
		Ctx.DbFnCtx??= new DbFnCtx();
		return await Dao.PageUserLang(Ctx.DbFnCtx, Ctx.UserCtx.UserId, Req, Ct);
	}
	
	public async Task<nil> BatUpdUserLang(
		IDbUserCtx Ctx, IAsyncEnumerable<PoUserLang> Pos, CT Ct
	){
		Pos = Pos.CheckOwner(Ctx.UserCtx.UserId);
		
		try{
			await SqlCmdMkr.RunInTxnIfNoCtx(Ctx.DbFnCtx, Ct, async(ctx)=>{
				
				await RepoUserLang.BatUpd(ctx, Pos, Ct);
				await RepoUserLang.AsAppRepo().BatBizTouch(ctx,Pos.Select(x=>x.Id),Ct);
				return NIL;
			});
			return NIL;
		}
		catch(AppErr ex) when(ReferenceEquals(ex.Type, ItemsErr.Common.PermissionDenied)){
			throw;
		}
		catch(Exception ex){
			var e = ItemsErr.Common.DataIllegalOrConflict.ToErr();
			e.AddErr(ex);
			throw e;
		}
	}

	public async Task<nil> BatAddUserLang(
		IDbUserCtx Ctx, IAsyncEnumerable<PoUserLang> Pos, CT Ct
	){
		Pos = Pos.CheckOwner(Ctx.UserCtx.UserId);
		Pos = Pos.Touch();
		try{
			await SqlCmdMkr.RunInTxnIfNoCtx(Ctx.DbFnCtx, Ct, async(ctx)=>{
				await RepoUserLang.BatAdd(ctx, Pos, Ct);
				return NIL;
			});
			return NIL;
		}
		catch(AppErr ex) when(ReferenceEquals(ex.Type, ItemsErr.Common.PermissionDenied)){
			throw;
		}
		catch(Exception ex){
			var e = ItemsErr.Common.DataIllegalOrConflict.ToErr();
			e.AddErr(ex);
			throw e;
		}
	}

	public IAsyncEnumerable<str> GetUnregisteredUserLangs(
		IDbUserCtx Ctx, CT Ct
	){
		Ctx.DbFnCtx ??= new DbFnCtx();
		return Dao.GetUnregisteredUserLangs(Ctx.DbFnCtx, Ctx.UserCtx.UserId, Ct);
	}

	public async Task<nil> AddAllUnregisteredUserLangs(
		IDbUserCtx Ctx, CT Ct
	){
		try{
			await SqlCmdMkr.RunInTxnIfNoCtx(Ctx.DbFnCtx, Ct, async(ctx)=>{
				var owner = Ctx.UserCtx.UserId;
				var langs = Dao.GetUnregisteredUserLangs(ctx, owner, Ct);
				var pos = langs.Select(lang=>new PoUserLang{
					Owner = owner,
					UniqName = lang,
					RelLangType = ELangIdentType.Bcp47,
					RelLang = lang,
					Descr = "",
				});
				pos = pos.CheckOwner(owner);
				pos = pos.Touch();
				await RepoUserLang.BatAdd(ctx, pos, Ct);
				return NIL;
			});
			return NIL;
		}
		catch(AppErr ex) when(ReferenceEquals(ex.Type, ItemsErr.Common.PermissionDenied)){
			throw;
		}
		catch(Exception ex){
			var e = ItemsErr.Common.DataIllegalOrConflict.ToErr();
			e.AddErr(ex);
			throw e;
		}
	}
}
