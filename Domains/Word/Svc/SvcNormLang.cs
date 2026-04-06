using Ngaq.Core.Infra;
using Ngaq.Core.Infra.Errors;
using Ngaq.Core.Shared.Base.Models.Po;
using Ngaq.Core.Shared.Dictionary.Models;
using Ngaq.Core.Shared.Dictionary.Models.Po.NormLang;
using Ngaq.Core.Shared.Word.Models.Dto;
using Ngaq.Core.Shared.Word.Svc;
using Ngaq.Local.Db.TswG;
using Ngaq.Local.Domains.Word.Dao;
using Tsinswreng.CsErr;
using Tsinswreng.CsPage;
using Tsinswreng.CsSql;
using Tsinswreng.CsTools;

namespace Ngaq.Local.Domains.Word.Svc;

public class SvcNormLang : ISvcNormLang{
	private readonly DaoNormLang Dao;
	private readonly ISqlCmdMkr SqlCmdMkr;
	private readonly IRepo<PoNormLang, IdNormLang> RepoNormLang;

	public SvcNormLang(
		DaoNormLang Dao,
		ISqlCmdMkr SqlCmdMkr,
		IRepo<PoNormLang, IdNormLang> RepoNormLang
	){
		this.Dao = Dao;
		this.SqlCmdMkr = SqlCmdMkr;
		this.RepoNormLang = RepoNormLang;
	}

	public IAsyncEnumerable<PoNormLang?> BatGetNormLangByTypeCode(
		IDbUserCtx Ctx,
		IAsyncEnumerable<(ELangIdentType, str)> Type_Code,
		CT Ct
	){
		Ctx.DbFnCtx ??= new DbFnCtx();
		return Dao.BatGetNormLangByTypeCode(Ctx.DbFnCtx, Ctx.UserCtx.UserId, Type_Code, Ct);
	}

	public async Task<IPageAsyE<PoNormLang>> PageNormLang(
		IDbUserCtx Ctx,
		ReqPageNormLang Req,
		CT Ct
	){
		Ctx.DbFnCtx ??= new DbFnCtx();
		return await Dao.PageNormLang(Ctx.DbFnCtx, Ctx.UserCtx.UserId, Req, Ct);
	}

	public async Task<nil> BatAddNormLang(
		IDbUserCtx Ctx,
		IAsyncEnumerable<PoNormLang> Pos,
		CT Ct
	) => await DoWrite(Ctx, Pos, needTouch: true, async (DbCtx, Repo, Ents, Ct2)=>{
		await Repo.BatAdd(DbCtx, Ents, Ct2);
	}, Ct);

	public async Task<nil> BatUpdNormLang(
		IDbUserCtx Ctx,
		IAsyncEnumerable<PoNormLang> Pos,
		CT Ct
	) => await DoWrite(Ctx, Pos, needTouch: false, async (DbCtx, Repo, Ents, Ct2)=>{
		await Repo.BatUpd(DbCtx, Ents, Ct2);
		await Repo.AsAppRepo().BatBizTouch(DbCtx, Ents.Select(x=>x.Id), Ct2);
	}, Ct);

	public async Task<nil> BatSoftDelNormLang(
		IDbUserCtx Ctx,
		IAsyncEnumerable<PoNormLang> Pos,
		CT Ct
	) => await DoWrite(Ctx, Pos, needTouch: false, async (DbCtx, Repo, Ents, Ct2)=>{
		await Repo.BatSoftDelById(DbCtx, Ents.Select(x=>x.Id), Ct2);
	}, Ct);

	public async Task<nil> InitBuiltinNormLang(IDbUserCtx Ctx, CT Ct){
		var Owner = Ctx.UserCtx.UserId;
		var Seeds = InitNormLang.GetNormLangList().Select(x=>new PoNormLang{
			Owner = Owner,
			Type = x.Type,
			Code = x.Code,
			NativeName = x.NativeName,
		}).ToList();
		if(Seeds.Count == 0){
			return NIL;
		}

		var Exists = BatGetNormLangByTypeCode(
			Ctx,
			ToolAsyE.ToAsyE(Seeds.Select(x=>(x.Type, x.Code))),
			Ct
		);
		var ToAdd = new List<PoNormLang>();
		var i = 0;
		await foreach(var Po in Exists.WithCancellation(Ct)){
			if(i >= Seeds.Count){
				break;
			}
			if(Po is null){
				ToAdd.Add(Seeds[i]);
			}
			i++;
		}
		if(ToAdd.Count == 0){
			return NIL;
		}
		await BatAddNormLang(Ctx, ToolAsyE.ToAsyE(ToAdd), Ct);
		return NIL;
	}

	private async Task<nil> DoWrite(
		IDbUserCtx Ctx,
		IAsyncEnumerable<PoNormLang> Pos,
		bool needTouch,
		Func<IDbFnCtx, IRepo<PoNormLang, IdNormLang>, IAsyncEnumerable<PoNormLang>, CT, Task> Operation,
		CT Ct
	){
		Pos = Pos.CheckOwner(Ctx.UserCtx.UserId);
		if(needTouch){
			Pos = Pos.Touch();
		}
		try{
			await SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async DbCtx=>{
				await Operation(DbCtx, RepoNormLang, Pos, Ct);
				return NIL;
			});
			return NIL;
		}
		catch(AppErr){
			throw;
		}
		catch(Exception Ex){
			var E = ItemsErr.Common.DataIllegalOrConflict.ToErr();
			E.AddErr(Ex);
			throw E;
		}
	}
}
