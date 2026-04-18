using Ngaq.Core.Infra;
using Ngaq.Core.Infra.Errors;
using Ngaq.Core.Shared.Base.Models.Po;
using Ngaq.Core.Shared.Dictionary.Models;
using Ngaq.Core.Shared.Word.Models.Dto;
using Ngaq.Core.Shared.Word.Models.Po.NormLangToUserLang;
using Ngaq.Core.Shared.Word.Svc;
using Ngaq.Backend.Db.TswG;
using Ngaq.Backend.Domains.Word.Dao;
using Tsinswreng.CsErr;
using Tsinswreng.CsPage;
using Tsinswreng.CsSql;
using Tsinswreng.CsTools;

namespace Ngaq.Backend.Domains.Word.Svc;

public class SvcNormLangToUserLang : ISvcNormLangToUserLang{
	private readonly DaoNormLangToUserLang Dao;
	private readonly ISqlCmdMkr SqlCmdMkr;
	private readonly IRepo<PoNormLangToUserLang, IdNormLangToUserLang> RepoNormLangToUserLang;

	public SvcNormLangToUserLang(
		DaoNormLangToUserLang Dao,
		ISqlCmdMkr SqlCmdMkr,
		IRepo<PoNormLangToUserLang, IdNormLangToUserLang> RepoNormLangToUserLang
	){
		this.Dao = Dao;
		this.SqlCmdMkr = SqlCmdMkr;
		this.RepoNormLangToUserLang = RepoNormLangToUserLang;
	}

	public async Task<str?> GetUserLangByNormLang(
		IDbUserCtx Ctx,
		ELangIdentType NormLangType,
		str NormLang,
		CT Ct
	){
		Ctx.DbFnCtx ??= new DbFnCtx();
		return await Dao.GetUserLangByNormLang(
			Ctx.DbFnCtx,
			Ctx.UserCtx.UserId,
			NormLangType,
			NormLang,
			Ct
		);
	}

	public async Task<IPageAsyE<PoNormLangToUserLang>> PageNormLangToUserLang(
		IDbUserCtx Ctx,
		ReqPageNormLangToUserLang Req,
		CT Ct
	){
		Ctx.DbFnCtx ??= new DbFnCtx();
		return await Dao.PageNormLangToUserLang(Ctx.DbFnCtx, Ctx.UserCtx.UserId, Req, Ct);
	}

	public async Task<nil> BatAddNormLangToUserLang(
		IDbUserCtx Ctx,
		IAsyncEnumerable<PoNormLangToUserLang> Pos,
		CT Ct
	) => await DoWrite(Ctx, Pos, needTouch: true, async (DbCtx, Repo, Ents, Ct2)=>{
		await Repo.BatAdd(DbCtx, Ents, Ct2);
	}, Ct);

	public async Task<nil> BatUpdNormLangToUserLang(
		IDbUserCtx Ctx,
		IAsyncEnumerable<PoNormLangToUserLang> Pos,
		CT Ct
	) => await DoWrite(Ctx, Pos, needTouch: false, async (DbCtx, Repo, Ents, Ct2)=>{
		await Repo.BatUpd(DbCtx, Ents, Ct2);
		await Repo.AsAppRepo().BatBizTouch(DbCtx, Ents.Select(x=>x.Id), Ct2);
	}, Ct);

	public async Task<nil> BatSoftDelNormLangToUserLang(
		IDbUserCtx Ctx,
		IAsyncEnumerable<PoNormLangToUserLang> Pos,
		CT Ct
	) => await DoWrite(Ctx, Pos, needTouch: false, async (DbCtx, Repo, Ents, Ct2)=>{
		await Repo.BatSoftDelById(DbCtx, Ents.Select(x=>x.Id), Ct2);
	}, Ct);

	private async Task<nil> DoWrite(
		IDbUserCtx Ctx,
		IAsyncEnumerable<PoNormLangToUserLang> Pos,
		bool needTouch,
		Func<IDbFnCtx, IRepo<PoNormLangToUserLang, IdNormLangToUserLang>, IAsyncEnumerable<PoNormLangToUserLang>, CT, Task> Operation,
		CT Ct
	){
		Pos = Pos.CheckOwner(Ctx.UserCtx.UserId);
		if(needTouch){
			Pos = Pos.Touch();
		}
		try{
			await SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async DbCtx=>{
				await Operation(DbCtx, RepoNormLangToUserLang, Pos, Ct);
				return NIL;
			});
			return NIL;
		}
		catch(AppErr){
			throw;
		}
		catch(Exception Ex){
			var E = KeysErr.Common.DataIllegalOrConflict.ToErr();
			E.AddErr(Ex);
			throw E;
		}
	}
}
