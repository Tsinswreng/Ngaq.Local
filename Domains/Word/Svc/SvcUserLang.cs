using Ngaq.Core.Infra;
using Ngaq.Core.Infra.Errors;
using Ngaq.Core.Shared.Dictionary.Models;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.Word.Models.Dto;
using Ngaq.Core.Shared.Word.Models.Po.UserLang;
using Ngaq.Core.Shared.Word.Svc;
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
		return await SqlCmdMkr.RunInTxnIfNoCtx(Ctx.DbFnCtx, Ct, async(dbCtx)=>{
			var owner = Ctx.UserCtx.UserId;
			await using var batch = new BatchCollector<PoUserLang, nil>(async(rows, ct)=>{
				await ThrowIfAnyNotOwned(dbCtx, owner, rows, ct);
				var now = Tempus.Now();
				foreach(var row in rows){
					row.Owner = owner;
					row.BizUpdatedAt = now;
				}
				await RepoUserLang.BatUpd(dbCtx, ToolAsyE.ToAsyE(rows), ct);
				return NIL;
			});
			await batch.ConsumeAll(Pos, Ct);
			return NIL;
		});
	}

	public async Task<nil> BatAddUserLang(
		IDbUserCtx Ctx, IAsyncEnumerable<PoUserLang> Pos, CT Ct
	){
		return await SqlCmdMkr.RunInTxnIfNoCtx(Ctx.DbFnCtx, Ct, async(dbCtx)=>{
			var owner = Ctx.UserCtx.UserId;
			await using var batch = new BatchCollector<PoUserLang, nil>(async(rows, ct)=>{
				var now = Tempus.Now();
				foreach(var row in rows){
					row.Owner = owner;
					row.BizUpdatedAt = now;
				}
				await RepoUserLang.BatAdd(dbCtx, ToolAsyE.ToAsyE(rows), ct);
				return NIL;
			});
			await batch.ConsumeAll(Pos, Ct);
			return NIL;
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
				var now = Tempus.Now();
				foreach(var row in rows){
					row.BizUpdatedAt = now;
				}
				await RepoUserLang.BatAdd(dbCtx, ToolAsyE.ToAsyE(rows), ct);
				return NIL;
			});
			await batch.ConsumeAll(pos, Ct);
			return NIL;
		});
	}

	async Task<nil> ThrowIfAnyNotOwned(
		IDbFnCtx DbCtx
		,IdUser Owner
		,IList<PoUserLang> Rows
		,CT Ct
	){
		var ids = Rows.Select(x=>x.Id);
		var gotRows = RepoUserLang.BatGetByIdWithDel(DbCtx, ToolAsyE.ToAsyE(ids), Ct);
		var i = 0;
		await foreach(var got in gotRows){
			if(i >= Rows.Count){
				break;
			}
			if(got is null || got.Owner != Owner){
				throw ItemsErr.Common.PermissionDenied.ToErr();
			}
			i++;
		}
		if(i != Rows.Count){
			throw ItemsErr.Common.PermissionDenied.ToErr();
		}
		return NIL;
	}
}
