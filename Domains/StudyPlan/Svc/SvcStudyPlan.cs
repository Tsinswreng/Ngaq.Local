using Ngaq.Core.Frontend.Kv;
using Ngaq.Core.Infra.IF;
using Ngaq.Core.Shared.Base.Models.Po;
using Ngaq.Core.Shared.Kv.Models;
using Ngaq.Core.Shared.Kv.Svc;
using Ngaq.Core.Shared.StudyPlan.Models.Req;
using Ngaq.Core.Shared.StudyPlan.Models.Po.PreFilter;
using Ngaq.Core.Shared.StudyPlan.Models.Po.StudyPlan;
using Ngaq.Core.Shared.StudyPlan.Models.Po.WeightArg;
using Ngaq.Core.Shared.StudyPlan.Models.Po.WeightCalculator;
using Ngaq.Core.Shared.StudyPlan.Models.PreFilter;
using Ngaq.Core.Shared.StudyPlan.Svc;
using Ngaq.Core.Shared.Word.Svc;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.User.UserCtx;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Tools.Json;
using Ngaq.Local.Domains.StudyPlan.Dao;
using Tsinswreng.CsPage;
using Tsinswreng.CsSql;
using Tsinswreng.CsTools;
using Ngaq.Core.Infra;
using Ngaq.Core.Word.Svc;
using Ngaq.Core.Shared.StudyPlan.Models;
using System.Text.Json;
using Ngaq.Core.Tools;
using Ngaq.Core.Shared.Word.WeightAlgo;
using Ngaq.Core.Shared.Word.WeightAlgo.Models;
using Tsinswreng.CsCore;
using Ngaq.Core.Infra.Errors;
using Tsinswreng.CsErr;

namespace Ngaq.Local.Domains.StudyPlan.Svc;

public partial class SvcStudyPlan:ISvcStudyPlan, IStudyPlanGetter{
	ISvcKv SvcKv;
	DaoStudyPlan DaoStudyPlan;
	ISqlCmdMkr SqlCmdMkr;
	IRepo<PoStudyPlan, IdStudyPlan> RepoStudyPlan;
	IRepo<PoWeightArg, IdWeightArg> RepoWeightArg;
	IRepo<PoWeightCalculator, IdWeightCalculator> RepoWeightCalculator;
	IRepo<PoPreFilter, IdPreFilter> RepoPreFilter;
	IRepo<PoKv, IdKv> RepoKv;
	IJsonSerializer JsonS;
	BoStudyPlan? CurBoStudyPlanCache = null;
	public SvcStudyPlan(
		ISvcKv SvcKv
		,DaoStudyPlan DaoStudyPlan
		,ISqlCmdMkr SqlCmdMkr
		,IRepo<PoStudyPlan, IdStudyPlan> RepoStudyPlan
		,IRepo<PoWeightArg, IdWeightArg> RepoWeightArg
		,IRepo<PoWeightCalculator, IdWeightCalculator> RepoWeightCalculator
		,IRepo<PoPreFilter, IdPreFilter> RepoPreFilter
		,IRepo<PoKv, IdKv> RepoKv
		,IJsonSerializer JsonSerializer
	){
		this.SvcKv = SvcKv;
		this.DaoStudyPlan = DaoStudyPlan;
		this. SqlCmdMkr = SqlCmdMkr;
		this.RepoStudyPlan = RepoStudyPlan;
		this.RepoWeightArg = RepoWeightArg;
		this.RepoWeightCalculator = RepoWeightCalculator;
		this.RepoPreFilter = RepoPreFilter;
		this.RepoKv = RepoKv;
		this.JsonS = JsonSerializer;
	}

	public async Task<BoStudyPlan> GetStudyPlan(IUserCtx User, CT Ct){
		await EnsureCurStudyPlan(User.ToDbUserCtx(), Ct);
		var studyPlan = await GetCurBoStudyPlan(User.ToDbUserCtx(), Ct);
		if(studyPlan is null){
			return new BoStudyPlan{
				WeightCalctr = new DfltWeightCalculator(),
				WeightArg = new Dictionary<str, obj?>(),
			};
		}
		studyPlan.WeightCalctr ??= new DfltWeightCalculator();
		studyPlan.WeightArg ??= new Dictionary<str, obj?>();
		return studyPlan;
	}

	public async Task<nil> SetCurStudyPlanId(
		IDbUserCtx Ctx, IdStudyPlan StudyPlanId, CT Ct
	){
		return await SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(ctx)=>{
			var key = KeysClientKv.CurStudyPlanId+"";
			var owner = Ctx.UserCtx.UserId;
			var oldKv = await SvcKv.BatGetByOwnerEtKStr(
				ctx
				,ToolAsyE.ToAsyE([(owner, key)])
				,Ct
			).FirstOrDefaultAsync(Ct);

			if(oldKv is null){
				var kv = new PoKv{
					Owner = owner,
				}.SetStrStr(key, StudyPlanId+"");
				await RepoKv.BatAdd(ctx, ToolAsyE.ToAsyE([kv]), Ct);
				return NIL;
			}

			oldKv.Owner = owner;
			oldKv.SetStrStr(key, StudyPlanId+"");
			await RepoKv.BatUpd(ctx, ToolAsyE.ToAsyE([oldKv]), Ct);
			return NIL;
		});
	}

	public IAsyncEnumerable<T> EnsureOwner<T>(
		IdUser UserId
		,IAsyncEnumerable<T> Pos
	)
		where T:I_Owner
	{
		Pos = Pos.Select(x=>{
			x.Owner = UserId;
			return x;
		});
		return Pos;
	}

	static void ThrowMappedStudyPlanErr(
		IErrNode ErrType, Exception Ex
	){
		if(
			Ex is AppErr appErr
			&& (
				ReferenceEquals(appErr.Type, ErrType)
				|| ReferenceEquals(appErr.Type, ItemsErr.Common.PermissionDenied)
			)
		){
			throw Ex;
		}
		var err = ErrType.ToErr();
		err.AddErr(Ex);
		throw err;
	}

	async Task<nil> WrapStudyPlanErr(
		IErrNode ErrType
		,Func<Task<nil>> Fn
	){
		try{
			return await Fn();
		}
		catch(Exception ex){
			ThrowMappedStudyPlanErr(ErrType, ex);
			throw;
		}
	}
	
	public async Task<nil> BatAddPreFilter(
		IDbUserCtx Ctx
		, IAsyncEnumerable<PoPreFilter> Pos
		,CT Ct
	){
		return await WrapStudyPlanErr(ItemsErr.StudyPlan.AddFailedDataMayConflict, async()=>{
			return await SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(ctx)=>{
				Pos = EnsureOwner(Ctx.UserCtx.UserId, Pos);
				await RepoPreFilter.BatAdd(ctx, Pos, Ct);
				return NIL;
			});
		});
	}
	
	public async Task<nil> BatAddStudyPlan(
		IDbUserCtx Ctx
		, IAsyncEnumerable<PoStudyPlan> Pos
		,CT Ct
	){
		return await WrapStudyPlanErr(ItemsErr.StudyPlan.AddFailedDataMayConflict, async()=>{
			return await SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(ctx)=>{
				Pos = EnsureOwner(Ctx.UserCtx.UserId, Pos);
				await RepoStudyPlan.BatAdd(ctx, Pos, Ct);
				CurBoStudyPlanCache = null;
				return NIL;
			});
		});
	}
	
	public async Task<nil> BatAddWeightArg(
		IDbUserCtx Ctx
		, IAsyncEnumerable<PoWeightArg> Pos
		,CT Ct
	){
		return await WrapStudyPlanErr(ItemsErr.StudyPlan.AddFailedDataMayConflict, async()=>{
			return await SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(ctx)=>{
				Pos = EnsureOwner(Ctx.UserCtx.UserId, Pos);
				await RepoWeightArg.BatAdd(ctx, Pos, Ct);
				return NIL;
			});
		});
	}
	
	public async Task<nil> BatAddWeightCalculator(
		IDbUserCtx Ctx
		, IAsyncEnumerable<PoWeightCalculator> Pos
		,CT Ct
	){
		return await WrapStudyPlanErr(ItemsErr.StudyPlan.AddFailedDataMayConflict, async()=>{
			return await SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(ctx)=>{
				Pos = EnsureOwner(Ctx.UserCtx.UserId, Pos);
				await RepoWeightCalculator.BatAdd(ctx, Pos, Ct);
				return NIL;
			});
		});
	}

	public async Task<IPageAsyE<PoStudyPlan>> PageStudyPlan(
		IDbUserCtx Ctx
		,ReqPageStudyPlan Req
		,CT Ct
	){
		Ctx.DbFnCtx??=new DbFnCtx();
		return await DaoStudyPlan.PageStudyPlan(Ctx.DbFnCtx, Ctx.UserCtx.UserId, Req, Ct);
	}

	public async Task<IPageAsyE<PoPreFilter>> PagePreFilter(
		IDbUserCtx Ctx
		,ReqPagePreFilter Req
		,CT Ct
	){
		Ctx.DbFnCtx??=new DbFnCtx();
		return await DaoStudyPlan.PagePreFilter(Ctx.DbFnCtx, Ctx.UserCtx.UserId, Req, Ct);
	}

	public async Task<IPageAsyE<PoWeightArg>> PageWeightArg(
		IDbUserCtx Ctx
		,ReqPageWeightArg Req
		,CT Ct
	){
		Ctx.DbFnCtx??=new DbFnCtx();
		return await DaoStudyPlan.PageWeightArg(Ctx.DbFnCtx, Ctx.UserCtx.UserId, Req, Ct);
	}

	public async Task<IPageAsyE<PoWeightCalculator>> PageWeightCalculator(
		IDbUserCtx Ctx
		,ReqPageWeightCalculator Req, CT Ct
	){
		Ctx.DbFnCtx??=new DbFnCtx();
		return await DaoStudyPlan.PageWeightCalculator(Ctx.DbFnCtx, Ctx.UserCtx.UserId, Req, Ct);
	}
	
	
	public async Task<IdStudyPlan?> GetCurStudyPlanId(IDbUserCtx Ctx, CT Ct){
		Ctx.DbFnCtx ??= new DbFnCtx();
		var kv = await SvcKv.BatGetByOwnerEtKStr(
			Ctx.DbFnCtx
			,ToolAsyE.ToAsyE([(Ctx.UserCtx.UserId, KeysClientKv.CurStudyPlanId+"")])
			,Ct
		).FirstOrDefaultAsync(Ct);
		if(kv is null || string.IsNullOrEmpty(kv.VStr)){
			return null;
		}
		if(IdStudyPlan.TryParse(kv.VStr, out var id)){
			return id;
		}
		return null;
	}

	public async Task<IWeightCalctr?> GetCurWeightCalctr(IDbUserCtx Ctx, CT Ct) {
		var bo = await GetCurBoStudyPlan(Ctx, Ct);
		return bo?.WeightCalctr;
	}

	public async Task<JnStudyPlan?> GetCurJnStudyPlan(IDbUserCtx Ctx, CT Ct) {
		Ctx.DbFnCtx ??= new DbFnCtx();
		var curStudyPlanId = await GetCurStudyPlanId(Ctx, Ct);
		if(curStudyPlanId is not IdStudyPlan studyPlanId || studyPlanId.IsNullOrDefault()){
			return null;
		}

		var jnStudyPlan = await DaoStudyPlan.BatGetStudyPlanById(
			Ctx.DbFnCtx
			,ToolAsyE.ToAsyE([studyPlanId])
			,Ct
		).FirstOrDefaultAsync(Ct);
		if(jnStudyPlan is null){
			return null;
		}

		if(jnStudyPlan.StudyPlan.Owner != Ctx.UserCtx.UserId){
			return null;
		}

		if(jnStudyPlan.PreFilter is null && !jnStudyPlan.StudyPlan.PreFilterId.IsNullOrDefault()){
			jnStudyPlan.PreFilter = await RepoPreFilter.BatGetByIdWithDel(
				Ctx.DbFnCtx
				,ToolAsyE.ToAsyE([jnStudyPlan.StudyPlan.PreFilterId])
				,Ct
			).FirstOrDefaultAsync(Ct);
		}
		if(jnStudyPlan.WeightCalculator is null && !jnStudyPlan.StudyPlan.WeightCalculatorId.IsNullOrDefault()){
			jnStudyPlan.WeightCalculator = await RepoWeightCalculator.BatGetByIdWithDel(
				Ctx.DbFnCtx
				,ToolAsyE.ToAsyE([jnStudyPlan.StudyPlan.WeightCalculatorId])
				,Ct
			).FirstOrDefaultAsync(Ct);
		}
		if(jnStudyPlan.WeightArg is null && !jnStudyPlan.StudyPlan.WeightArgId.IsNullOrDefault()){
			jnStudyPlan.WeightArg = await RepoWeightArg.BatGetByIdWithDel(
				Ctx.DbFnCtx
				,ToolAsyE.ToAsyE([jnStudyPlan.StudyPlan.WeightArgId])
				,Ct
			).FirstOrDefaultAsync(Ct);
		}

		if(jnStudyPlan.PreFilter is { } poPreFilter && poPreFilter.Owner != Ctx.UserCtx.UserId){
			jnStudyPlan.PreFilter = null;
		}
		if(jnStudyPlan.WeightCalculator is { } poWeightCalculator && poWeightCalculator.Owner != Ctx.UserCtx.UserId){
			jnStudyPlan.WeightCalculator = null;
		}
		if(jnStudyPlan.WeightArg is { } poWeightArg && poWeightArg.Owner != Ctx.UserCtx.UserId){
			jnStudyPlan.WeightArg = null;
		}

		return jnStudyPlan;
	}

	public async Task<BoStudyPlan?> GetCurBoStudyPlan(IDbUserCtx Ctx, CT Ct) {
		Ctx.DbFnCtx ??= new DbFnCtx();
		var curStudyPlanId = await GetCurStudyPlanId(Ctx, Ct);
		if(curStudyPlanId is not IdStudyPlan studyPlanId || studyPlanId.IsNullOrDefault()){
			CurBoStudyPlanCache = null;
			return null;
		}

		if(
			CurBoStudyPlanCache?.PoStudyPlan is { } cachedPoStudyPlan
			&& cachedPoStudyPlan.Id == studyPlanId
		){
			var latestPoStudyPlan = await RepoStudyPlan.BatGetByIdWithDel(
				Ctx.DbFnCtx
				,ToolAsyE.ToAsyE([studyPlanId])
				,Ct
			).FirstOrDefaultAsync(Ct);

			if(
				latestPoStudyPlan is not null
				&& latestPoStudyPlan.Owner == Ctx.UserCtx.UserId
				&& latestPoStudyPlan.BizUpdatedAt == cachedPoStudyPlan.BizUpdatedAt
			){
				return CurBoStudyPlanCache;
			}
		}

		var jnStudyPlan = await GetCurJnStudyPlan(Ctx, Ct);
		if(jnStudyPlan is null){
			CurBoStudyPlanCache = null;
			return null;
		}

		var boStudyPlan = new BoStudyPlan{
			PoStudyPlan = jnStudyPlan.StudyPlan,
			PoPreFilter = jnStudyPlan.PreFilter,
			PoWeightArg = jnStudyPlan.WeightArg,
			PoWeightCalculator = jnStudyPlan.WeightCalculator,
		};

		if(
			boStudyPlan.PoPreFilter is { } poPreFilter
			&& poPreFilter.Type == EPreFilterType.Json
			&& !string.IsNullOrWhiteSpace(poPreFilter.Text)
		){
			var json = poPreFilter.Text;
			if(!string.IsNullOrWhiteSpace(json)){
				boStudyPlan.PreFilter = JsonS.Parse<PreFilter>(json);
			}
		}

		if(
			boStudyPlan.PoWeightArg is { } poWeightArg
			&& poWeightArg.Type == EWeightArgType.Json
			&& !string.IsNullOrWhiteSpace(poWeightArg.Text)
		){
			var json = poWeightArg.Text;
			if(!string.IsNullOrWhiteSpace(json)){
				try{
					boStudyPlan.WeightArg = ToolJson.JsonStrToDict(json);
				}catch(Exception e){
					throw ItemsErr.Word.WeightCalcGetStudyPlanFailed.ToErr()
						.AddErr(e)
						.AddDebugArgs(poWeightArg.Id, poWeightArg.UniqName, json);
				}
			}
		}

		boStudyPlan.WeightCalctr = MakeWeightCalctr(boStudyPlan.PoWeightCalculator);
		CurBoStudyPlanCache = boStudyPlan;
		return boStudyPlan;
	}

	IWeightCalctr? MakeWeightCalctr(PoWeightCalculator? PoWeightCalculator){
		if(PoWeightCalculator is null){
			return null;
		}
		if(PoWeightCalculator.Type == EWeightCalculatorType.Builtin){
			return new DfltWeightCalculator();
		}
		if(PoWeightCalculator.Type == EWeightCalculatorType.Js){
			if(string.IsNullOrWhiteSpace(PoWeightCalculator.Text)){
				throw ItemsErr.Word.WeightCalcInvalidAlgorithm.ToErr(
					PoWeightCalculator.Id,
					PoWeightCalculator.UniqName,
					"JsDataEmpty"
				);
			}
			var jsCode = PoWeightCalculator.Text;
			if(string.IsNullOrWhiteSpace(jsCode)){
				throw ItemsErr.Word.WeightCalcInvalidAlgorithm.ToErr(
					PoWeightCalculator.Id,
					PoWeightCalculator.UniqName,
					"JsCodeEmpty"
				);
			}
			return new JsWeightCalctr(JsonS, jsCode);
		}
		throw ItemsErr.Word.WeightCalcInvalidAlgorithm.ToErr(
			PoWeightCalculator.Id,
			PoWeightCalculator.UniqName,
			PoWeightCalculator.Type
		);
	}

	/// 生成「內置默認學習方案」的業務模型。
	/// 只在內存中構造，不直接操作資料庫。
	/// <param name="Ctx">資料庫/用戶上下文；此方法主要使用其中的 UserId 作 Owner。</param>
	/// <param name="Ct">取消令牌。</param>
	/// <returns>返回完整的內置 <see cref="BoStudyPlan"/>（含 PoStudyPlan/PoWeightCalculator/PoWeightArg 與可運行的默認計算器）。</returns>
	public Task<BoStudyPlan> GetDfltStudyPlan(
		IDbUserCtx Ctx, CT Ct
	){
		var owner = Ctx.UserCtx.UserId;
		// 內置資源命名都要求帶固定前綴，避免與用戶自定義名稱衝突。
		var builtinCalcName = Consts.BuiltinPrefix + DfltWeightCalculator.Name;
		var builtinArgName = Consts.BuiltinPrefix + DfltWeightCfg.Name;
		var builtinPlanName = Consts.BuiltinPrefix + "Default";

		// 構造默認權重配置，並序列化為 WeightArg 的 JSON 載荷。
		var cfg = new DfltWeightCfg();
		var cfgJson = JsonS.Stringify(cfg);
		var cfgDict = ToolJson.JsonStrToDict(cfgJson);
		// 內置算法：Type=BuiltIn，Text/Binary 留空，運行時直接用 DfltWeightCalculator。
		var poWeightCalculator = new PoWeightCalculator{
			Owner = owner,
			UniqName = builtinCalcName,
			Type = EWeightCalculatorType.Builtin,
			Text = null,
			Binary = null,
			Descr = "",
		};
		// 內置算法參數：Type=Json，Text 保存默認配置。
		var poWeightArg = new PoWeightArg{
			Owner = owner,
			UniqName = builtinArgName,
			Type = EWeightArgType.Json,
			Text = cfgJson,
			Binary = null,
			WeightCalculatorId = poWeightCalculator.Id,
			Descr = "",
		};
		// StudyPlan 用外鍵關聯到剛構造的算法與參數。
		var poStudyPlan = new PoStudyPlan{
			Owner = owner,
			UniqName = builtinPlanName,
			Descr = "",
			WeightCalculatorId = poWeightCalculator.Id,
			WeightArgId = poWeightArg.Id,
			PreFilterId = IdPreFilter.Zero,
		};

		var boStudyPlan = new BoStudyPlan{
			PoStudyPlan = poStudyPlan,
			PoWeightCalculator = poWeightCalculator,
			PoWeightArg = poWeightArg,
			PoPreFilter = null,
			PreFilter = null,
			WeightCalctr = new DfltWeightCalculator(),
			WeightArg = cfgDict,
		};
		return Task.FromResult(boStudyPlan);
	}

	public async Task<bool> EnsureCurStudyPlan(
		IDbUserCtx Ctx, CT Ct
	){
		return await SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(dbCtx)=>{
			var dbUserCtx = new DbUserCtx(Ctx.UserCtx, dbCtx);
			var owner = Ctx.UserCtx.UserId;

			// 1) 先驗證當前 CurStudyPlanId 是否有效且屬於當前用戶。
			var curStudyPlanId = await GetCurStudyPlanId(dbUserCtx, Ct);
			if(curStudyPlanId is IdStudyPlan curId && !curId.IsNullOrDefault()){
				var curPoStudyPlan = await RepoStudyPlan.BatGetByIdWithDel(
					dbCtx,
					ToolAsyE.ToAsyE([curId]),
					Ct
				).FirstOrDefaultAsync(Ct);
				if(curPoStudyPlan is not null && curPoStudyPlan.Owner == owner){
					return false;
				}
			}

			// 2) 若當前 id 失效，嘗試回退到用戶已有的最近一個方案。
			var req = new ReqPageStudyPlan{
				PageQry = new PageQry{
					PageIdx = 0,
					PageSize = 1,
				},
			};
			var page = await DaoStudyPlan.PageStudyPlan(dbCtx, owner, req, Ct);
			var latestStudyPlan = await page.DataAsyE.OrEmpty().FirstOrDefaultAsync(Ct);
			if(latestStudyPlan is not null){
				await SetCurStudyPlanId(dbUserCtx, latestStudyPlan.Id, Ct);
				CurBoStudyPlanCache = null;
				return false;
			}

			// 3) 用戶完全沒有方案時，創建並落庫內置默認方案。
			var builtinStudyPlan = await GetDfltStudyPlan(dbUserCtx, Ct);
			if(builtinStudyPlan.PoWeightCalculator is { } poWeightCalculator){
				await RepoWeightCalculator.BatAdd(dbCtx, ToolAsyE.ToAsyE([poWeightCalculator]), Ct);
			}
			if(builtinStudyPlan.PoWeightArg is { } poWeightArg){
				await RepoWeightArg.BatAdd(dbCtx, ToolAsyE.ToAsyE([poWeightArg]), Ct);
			}
			if(builtinStudyPlan.PoPreFilter is { } poPreFilter){
				await RepoPreFilter.BatAdd(dbCtx, ToolAsyE.ToAsyE([poPreFilter]), Ct);
			}
			if(builtinStudyPlan.PoStudyPlan is not { } poStudyPlan){
				return false;
			}
			await RepoStudyPlan.BatAdd(dbCtx, ToolAsyE.ToAsyE([poStudyPlan]), Ct);
			await SetCurStudyPlanId(dbUserCtx, poStudyPlan.Id, Ct);

			// 新創建的默認方案可直接作為快取，減少後續重查。
			CurBoStudyPlanCache = builtinStudyPlan;
			return true;
		});
	}

	static async IAsyncEnumerable<T> ToAsyE<T>(
		IEnumerable<T> Items
	){
		foreach(var item in Items){
			yield return item;
		}
	}

	async Task<nil> ThrowIfAnyNotOwned<TEntity, TId>(
		IDbUserCtx DbCtx
		,IRepo<TEntity, TId> Repo
		,IList<TEntity> Rows
		,CT Ct
	)
		where TEntity: class, I_Owner, I_Id<TId>, new()
	{
		var ids = Rows.Select(x=>x.Id);
		var gotRows = Repo.BatGetByIdWithDel(DbCtx.DbFnCtx!, ToolAsyE.ToAsyE(ids), Ct);
		var owner = DbCtx.UserCtx.UserId;
		await foreach(var (_, got) in gotRows.Index()){
			if(got is null || got.Owner != owner){
				throw ItemsErr.Common.PermissionDenied.ToErr();
			}
		}
		return NIL;
	}

	async Task<nil> BatUpdWithOwnerCheck<TEntity, TId>(
		IDbUserCtx Ctx
		,IRepo<TEntity, TId> Repo
		,IAsyncEnumerable<TEntity> Pos
		,CT Ct
	)
		where TEntity: class, I_Owner, I_Id<TId>, new()
	{
		await SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(ctx)=>{
			var dbUserCtx = new DbUserCtx(Ctx.UserCtx, ctx);
			var owner = Ctx.UserCtx.UserId;
			await using var batch = new BatchCollector<TEntity, nil>(async(rows, ct)=>{
				await ThrowIfAnyNotOwned(dbUserCtx, Repo, rows, ct);
				foreach(var row in rows){
					row.Owner = owner;
				}
				await Repo.BatUpd(ctx, ToolAsyE.ToAsyE(rows), ct);
				return NIL;
			});
			await batch.ConsumeAll(Pos, Ct);
			return NIL;
		});
		return NIL;
	}

	async Task<nil> BatSoftDelWithOwnerCheck<TEntity, TId>(
		IDbUserCtx Ctx
		,IRepo<TEntity, TId> Repo
		,IAsyncEnumerable<TEntity> Pos
		,CT Ct
	)
		where TEntity: class, I_Owner, I_Id<TId>, new()
	{
		await SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(ctx)=>{
			var dbUserCtx = new DbUserCtx(Ctx.UserCtx, ctx);
			await using var batch = new BatchCollector<TEntity, nil>(async(rows, ct)=>{
				await ThrowIfAnyNotOwned(dbUserCtx, Repo, rows, ct);
				var ids = rows.Select(x=>x.Id);
				await Repo.BatSoftDelById(ctx, ToolAsyE.ToAsyE(ids), ct);
				return NIL;
			});
			await batch.ConsumeAll(Pos, Ct);
			return NIL;
		});
		return NIL;
	}
	
	public async Task<nil> BatUpdPreFilter(
		IDbUserCtx Ctx
		,IAsyncEnumerable<PoPreFilter> Pos
		,CT Ct
	){
		return await WrapStudyPlanErr(ItemsErr.StudyPlan.UpdateFailedDataMayConflict, async()=>{
			await BatUpdWithOwnerCheck(Ctx, RepoPreFilter, Pos, Ct);
			return NIL;
		});
	}

	/// 批量更新權重算法。
	/// 參照 BatUpdPreFilter 的寫法：先校驗 Owner，再在事務內批量更新。
	public async Task<nil> BatUpdWeightCalculator(
		IDbUserCtx Ctx, IAsyncEnumerable<PoWeightCalculator> Pos, CT Ct
	){
		return await WrapStudyPlanErr(ItemsErr.StudyPlan.UpdateFailedDataMayConflict, async()=>{
			await BatUpdWithOwnerCheck(Ctx, RepoWeightCalculator, Pos, Ct);
			return NIL;
		});
	}

	/// 批量更新權重參數。
	/// 參照 BatUpdPreFilter 的寫法：先校驗 Owner，再在事務內批量更新。
	public async Task<nil> BatUpdWeightArg(
		IDbUserCtx Ctx, IAsyncEnumerable<PoWeightArg> Pos, CT Ct
	){
		return await WrapStudyPlanErr(ItemsErr.StudyPlan.UpdateFailedDataMayConflict, async()=>{
			await BatUpdWithOwnerCheck(Ctx, RepoWeightArg, Pos, Ct);
			return NIL;
		});
	}

	/// 批量軟刪除前置篩選器（僅刪除根實體，不處理關聯）。
	/// 參照 BatUpdPreFilter 的寫法：先校驗 Owner，再在事務內批量軟刪除。
	public async Task<nil> BatSoftDelPreFilter(
		IDbUserCtx Ctx, IAsyncEnumerable<PoPreFilter> Pos, CT Ct
	){
		await BatSoftDelWithOwnerCheck(Ctx, RepoPreFilter, Pos, Ct);
		return NIL;
	}

	/// 批量軟刪除權重算法（僅刪除根實體，不處理關聯）。
	/// 參照 BatUpdPreFilter 的寫法：先校驗 Owner，再在事務內批量軟刪除。
	public async Task<nil> BatSoftDelWeightCalculator(
		IDbUserCtx Ctx, IAsyncEnumerable<PoWeightCalculator> Pos, CT Ct
	){
		await BatSoftDelWithOwnerCheck(Ctx, RepoWeightCalculator, Pos, Ct);
		return NIL;
	}

	/// 批量軟刪除權重參數（僅刪除根實體，不處理關聯）。
	/// 參照 BatUpdPreFilter 的寫法：先校驗 Owner，再在事務內批量軟刪除。
	public async Task<nil> BatSoftDelWeightArg(
		IDbUserCtx Ctx, IAsyncEnumerable<PoWeightArg> Pos, CT Ct
	){
		await BatSoftDelWithOwnerCheck(Ctx, RepoWeightArg, Pos, Ct);
		return NIL;
	}

	/// 批量更新學習方案。
	public async Task<nil> BatUpdStudyPlan(
		IDbUserCtx Ctx, IAsyncEnumerable<PoStudyPlan> Pos, CT Ct
	){
		return await WrapStudyPlanErr(ItemsErr.StudyPlan.UpdateFailedDataMayConflict, async()=>{
			await BatUpdWithOwnerCheck(Ctx, RepoStudyPlan, Pos, Ct);
			CurBoStudyPlanCache = null;
			return NIL;
		});
	}

	/// 批量軟刪除學習方案。僅標記 PoStudyPlan 本體，不處理關聯資產。
	public async Task<nil> BatSoftDelStudyPlan(
		IDbUserCtx Ctx, IAsyncEnumerable<PoStudyPlan> Pos, CT Ct
	){
		await BatSoftDelWithOwnerCheck(Ctx, RepoStudyPlan, Pos, Ct);
		CurBoStudyPlanCache = null;
		return NIL;
	}

	public Task<nil> RestoreBuiltinStudyPlan(IDbUserCtx Ctx, CT Ct) {
		return SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(dbCtx)=>{
			var dbUserCtx = new DbUserCtx(Ctx.UserCtx, dbCtx);
			var builtinStudyPlan = await GetDfltStudyPlan(dbUserCtx, Ct);
			var owner = Ctx.UserCtx.UserId;

			if(builtinStudyPlan.PoWeightCalculator is not { } poWeightCalculator){
				return NIL;
			}
			if(builtinStudyPlan.PoWeightArg is not { } poWeightArg){
				return NIL;
			}
			if(builtinStudyPlan.PoStudyPlan is not { } poStudyPlan){
				return NIL;
			}

			if(!string.IsNullOrEmpty(poWeightCalculator.UniqName)){
				var ids = DaoStudyPlan.BatSlctWeightCalculatorIdByOwnerUniqNameWithDel(
					dbCtx, owner, ToolAsyE.ToAsyE([poWeightCalculator.UniqName]), Ct
				).Where(x=>x is not null).Select(x=>x!.Value);
				await RepoWeightCalculator.BatSoftDelById(dbCtx, ids, Ct);
			}
			if(!string.IsNullOrEmpty(poWeightArg.UniqName)){
				var ids = DaoStudyPlan.BatSlctWeightArgIdByOwnerUniqNameWithDel(
					dbCtx, owner, ToolAsyE.ToAsyE([poWeightArg.UniqName]), Ct
				).Where(x=>x is not null).Select(x=>x!.Value);
				await RepoWeightArg.BatSoftDelById(dbCtx, ids, Ct);
			}
			if(!string.IsNullOrEmpty(poStudyPlan.UniqName)){
				var ids = DaoStudyPlan.BatSlctStudyPlanIdByOwnerUniqNameWithDel(
					dbCtx, owner, ToolAsyE.ToAsyE([poStudyPlan.UniqName]), Ct
				).Where(x=>x is not null).Select(x=>x!.Value);
				await RepoStudyPlan.BatSoftDelById(dbCtx, ids, Ct);
			}

			await RepoWeightCalculator.BatAdd(dbCtx, ToolAsyE.ToAsyE([poWeightCalculator]), Ct);
			await RepoWeightArg.BatAdd(dbCtx, ToolAsyE.ToAsyE([poWeightArg]), Ct);
			await RepoStudyPlan.BatAdd(dbCtx, ToolAsyE.ToAsyE([poStudyPlan]), Ct);
			await SetCurStudyPlanId(dbUserCtx, poStudyPlan.Id, Ct);

			CurBoStudyPlanCache = builtinStudyPlan;
			return NIL;
		});
	}
	
	public T? EnsureOwner<T>(T? Entity, IdUser Owner)
		where T:I_Owner
	{
		if(Entity is null){
			return default;
		}
		if(Entity.Owner != Owner){
			return default;
		}
		return Entity;
	}
	
	public IAsyncEnumerable<PoWeightCalculator?> BatGetWeightCalculatorById(
		IDbUserCtx Ctx, IAsyncEnumerable<IdWeightCalculator> Ids, CT Ct
	){
		Ctx.DbFnCtx??=new DbFnCtx();
		var pos = RepoWeightCalculator.BatGetById(Ctx.DbFnCtx, Ids, Ct);
		var r = pos.Select(x=>EnsureOwner(x, Ctx.UserCtx.UserId));
		return r;
	}
	
	public IAsyncEnumerable<PoPreFilter?> BatGetPreFilterById(
		IDbUserCtx Ctx, IAsyncEnumerable<IdPreFilter> Ids, CT Ct
	){
		Ctx.DbFnCtx??=new DbFnCtx();
		var pos = RepoPreFilter.BatGetById(Ctx.DbFnCtx, Ids, Ct);
		var r = pos.Select(x=>EnsureOwner(x, Ctx.UserCtx.UserId));
		return r;
	}
	
	public IAsyncEnumerable<PoWeightArg?> BatGetWeightArgById(
		IDbUserCtx Ctx, IAsyncEnumerable<IdWeightArg> Ids, CT Ct
	){
		Ctx.DbFnCtx??=new DbFnCtx();
		var pos = RepoWeightArg.BatGetById(Ctx.DbFnCtx, Ids, Ct);
		var r = pos.Select(x=>EnsureOwner(x, Ctx.UserCtx.UserId));
		return r;
	}

}
