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
				await RepoUserLang.AsAppRepo().BatBizTouch(ctx, Pos.Select(x=>x.Id), Ct);
				await RepoUserLang.BatUpd(ctx, Pos, Ct);
				return NIL;
			});
			return NIL;
		}
		catch (System.Exception ex){
			var e = ItemsErr.Common.DataIllegalOrConflict.ToErr();
			e.AddErr(ex);
			throw e;
		}
	}

	public async Task<nil> BatUpdUserLangOld(//不應該用BatchCollector分批
		IDbUserCtx Ctx, IAsyncEnumerable<PoUserLang> Pos, CT Ct
	){
		return await WrapSvcErr(ItemsErr.Common.DataIllegalOrConflict, async()=>{
			return await SqlCmdMkr.RunInTxnIfNoCtx(Ctx.DbFnCtx, Ct, async(dbCtx)=>{
				var owner = Ctx.UserCtx.UserId;
				await using var batch = new BatchCollector<PoUserLang, nil>(async(rows, ct)=>{
					var checkedRows = rows.CheckOwner(owner).ToList();
					var now = Tempus.Now();
					foreach(var row in checkedRows){
						row.BizUpdatedAt = now;
					}
					await RepoUserLang.BatUpd(dbCtx, ToolAsyE.ToAsyE(checkedRows), ct);
					return NIL;
				});
				await batch.ConsumeAll(Pos, Ct);
				return NIL;
			});
		});
	}

	public async Task<nil> BatAddUserLang(
		IDbUserCtx Ctx, IAsyncEnumerable<PoUserLang> Pos, CT Ct
	){
		return await WrapSvcErr(ItemsErr.Common.DataIllegalOrConflict, async()=>{
			return await SqlCmdMkr.RunInTxnIfNoCtx(Ctx.DbFnCtx, Ct, async(dbCtx)=>{
				var owner = Ctx.UserCtx.UserId;
				await using var batch = new BatchCollector<PoUserLang, nil>(async(rows, ct)=>{
					var checkedRows = rows.CheckOwner(owner).ToList();
					var now = Tempus.Now();
					foreach(var row in checkedRows){
						row.BizUpdatedAt = now;
					}
					await RepoUserLang.BatAdd(dbCtx, ToolAsyE.ToAsyE(checkedRows), ct);
					return NIL;
				});
				await batch.ConsumeAll(Pos, Ct);
				return NIL;
			});
		});
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
		return await WrapSvcErr(ItemsErr.Common.DataIllegalOrConflict, async()=>{
			return await SqlCmdMkr.RunInTxnIfNoCtx(Ctx.DbFnCtx, Ct, async(dbCtx)=>{
				var owner = Ctx.UserCtx.UserId;
				var langs = Dao.GetUnregisteredUserLangs(dbCtx, owner, Ct);
				var pos = langs.Select(lang=>new PoUserLang{
					Owner = owner,
					UniqName = lang,
					RelLangType = ELangIdentType.Bcp47,
					RelLang = lang,
					Descr = "",
				});
				await using var batch = new BatchCollector<PoUserLang, nil>(async(rows, ct)=>{
					var checkedRows = rows.CheckOwner(owner).ToList();
					var now = Tempus.Now();
					foreach(var row in checkedRows){
						row.BizUpdatedAt = now;
					}
					await RepoUserLang.BatAdd(dbCtx, ToolAsyE.ToAsyE(checkedRows), ct);
					return NIL;
				});
				await batch.ConsumeAll(pos, Ct);
				return NIL;
			});
		});
	}

	static void ThrowMappedSvcErr(IErrNode ErrType, Exception Ex){
		if(
			Ex is AppErr appErr
			&& ReferenceEquals(appErr.Type, ItemsErr.Common.PermissionDenied)
		){
			throw Ex;
		}
		var err = ErrType.ToErr();
		err.AddErr(Ex);
		throw err;
	}

	async Task<nil> WrapSvcErr(IErrNode ErrType, Func<Task<nil>> Fn){
		try{
			return await Fn();
		}
		catch(Exception ex){
			ThrowMappedSvcErr(ErrType, ex);
			throw;
		}
	}
}
