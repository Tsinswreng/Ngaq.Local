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
using Ngaq.Core.Shared.StudyPlan.Svc;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.User.UserCtx;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Local.Domains.StudyPlan.Dao;
using Tsinswreng.CsPage;
using Tsinswreng.CsSql;
using Tsinswreng.CsTools;
using Ngaq.Core.Infra;

namespace Ngaq.Local.Domains.StudyPlan.Svc;

public partial class SvcStudyPlan:ISvcStudyPlan{
	ISvcKv SvcKv;
	DaoStudyPlan DaoStudyPlan;
	ISqlCmdMkr SqlCmdMkr;
	IRepo<PoStudyPlan, IdStudyPlan> RepoStudyPlan;
	IRepo<PoWeightArg, IdWeightArg> RepoWeightArg;
	IRepo<PoWeightCalculator, IdWeightCalculator> RepoWeightCalculator;
	IRepo<PoPreFilter, IdPreFilter> RepoPreFilter;
	public SvcStudyPlan(
		ISvcKv SvcKv
		,DaoStudyPlan DaoStudyPlan
		,ISqlCmdMkr SqlCmdMkr
		,IRepo<PoStudyPlan, IdStudyPlan> RepoStudyPlan
		,IRepo<PoWeightArg, IdWeightArg> RepoWeightArg
		,IRepo<PoWeightCalculator, IdWeightCalculator> RepoWeightCalculator
		,IRepo<PoPreFilter, IdPreFilter> RepoPreFilter
	){
		this.SvcKv = SvcKv;
		this.DaoStudyPlan = DaoStudyPlan;
		this. SqlCmdMkr = SqlCmdMkr;
		this.RepoStudyPlan = RepoStudyPlan;
		this.RepoWeightArg = RepoWeightArg;
		this.RepoWeightCalculator = RepoWeightCalculator;
		this.RepoPreFilter = RepoPreFilter;
	}

	public async Task<nil> SetCurStudyPlanId(
		IDbUserCtx Ctx, IdStudyPlan StudyPlanId, CT Ct
	){
		return await SqlCmdMkr.RunInTxnIfNoCtx(Ctx.DbFnCtx, Ct, async(ctx)=>{
			var kv = new PoKv();
			kv.Owner = Ctx.UserCtx.UserId;
			kv.SetStrStr(KeysClientKv.CurStudyPlanId, StudyPlanId+"");
			return await SvcKv.BatSet(ctx, ToolAsyE.ToAsyE([kv]), Ct);
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
	
	public async Task<nil> BatAddPreFilter(
		IDbUserCtx Ctx
		, IAsyncEnumerable<PoPreFilter> Pos
		,CT Ct
	){
		return await SqlCmdMkr.RunInTxnIfNoCtx(Ctx.DbFnCtx, Ct, async(ctx)=>{
			Pos = EnsureOwner(Ctx.UserCtx.UserId, Pos);
			await RepoPreFilter.BatAdd(ctx, Pos, Ct);
			return NIL;
		});
	}
	
	public async Task<nil> BatAddWeightArg(
		IDbUserCtx Ctx
		, IAsyncEnumerable<PoWeightArg> Pos
		,CT Ct
	){
		return await SqlCmdMkr.RunInTxnIfNoCtx(Ctx.DbFnCtx, Ct, async(ctx)=>{
			Pos = EnsureOwner(Ctx.UserCtx.UserId, Pos);
			await RepoWeightArg.BatAdd(ctx, Pos, Ct);
			return NIL;
		});
	}
	
	public async Task<nil> BatAddWeightCalculator(
		IDbUserCtx Ctx
		, IAsyncEnumerable<PoWeightCalculator> Pos
		,CT Ct
	){
		return await SqlCmdMkr.RunInTxnIfNoCtx(Ctx.DbFnCtx, Ct, async(ctx)=>{
			Pos = EnsureOwner(Ctx.UserCtx.UserId, Pos);
			await RepoWeightCalculator.BatAdd(ctx, Pos, Ct);
			return NIL;
		});
	}

	public async Task<IPageAsyE<PoStudyPlan>> PageStudyPlan(
		IDbUserCtx Ctx
		,ReqPageStudyPlan Req
		,CT Ct
	){
		Ctx.DbFnCtx??=new DbFnCtx();
		return await DaoStudyPlan.PageStudyPlan(Ctx.DbFnCtx, Req, Ct);
	}

	public async Task<IPageAsyE<PoPreFilter>> PagePreFilter(
		IDbUserCtx Ctx
		,ReqPagePreFilter Req
		,CT Ct
	){
		Ctx.DbFnCtx??=new DbFnCtx();
		return await DaoStudyPlan.PagePreFilter(Ctx.DbFnCtx, Req, Ct);
	}

	public async Task<IPageAsyE<PoWeightArg>> PageWeightArg(
		IDbUserCtx Ctx
		,ReqPageWeightArg Req
		,CT Ct
	){
		Ctx.DbFnCtx??=new DbFnCtx();
		return await DaoStudyPlan.PageWeightArg(Ctx.DbFnCtx, Req, Ct);
	}

	public async Task<IPageAsyE<PoWeightCalculator>> PageWeightCalculator(
		IDbUserCtx Ctx
		,ReqPageWeightCalculator Req, CT Ct
	){
		Ctx.DbFnCtx??=new DbFnCtx();
		return await DaoStudyPlan.PageWeightCalculator(Ctx.DbFnCtx, Req, Ct);
	}
	
	
	public async Task<IdStudyPlan?> GetCurStudyPlanId(IDbUserCtx Ctx, CT Ct){
		var kv = await SvcKv.BatGetByOwnerEtKStr(
			Ctx.DbFnCtx
			,ToolAsyE.ToAsyE([(Ctx.UserCtx.UserId, KeysClientKv.CurStudyPlanId+"")])
			,Ct
		).FirstOrDefaultAsync(Ct);
		if(kv is null || string.IsNullOrEmpty(kv.VStr)){
			return null;
		}
		return IdStudyPlan.FromLow64Base(kv?.VStr??"");
	}
	

	#if false
public async Task<Func<
		IUserCtx, IdStudyPlan
		,CT, Task<BoStudyPlan?>
	>> FnGetStudyPlanById(IDbFnCtx Ctx, CT Ct){
		var fnPreFilter = await RepoPreFilter.FnSlctOneById(Ctx, Ct);
		var fnWeightArg = await RepoWeightArg.FnSlctOneById(Ctx, Ct);
		var fnWeightCalculator = await RepoWeightCalculator.FnSlctOneById(Ctx, Ct);
		var fnStudyPlan = await RepoStudyPlan.FnSlctOneById(Ctx, Ct);

		return async (UserCtx, StudyPlanId, Ct)=>{
			var studyPlan = await fnStudyPlan(StudyPlanId, Ct);
			if(studyPlan == null){
				return null;
			}
			var R = new BoStudyPlan();
			R.PoStudyPlan = studyPlan;

			// Get PreFilter
			if(!studyPlan.PreFilterId.IsNullOrDefault()){
				var poPreFilter = await fnPreFilter(studyPlan.PreFilterId, Ct);
				if(poPreFilter != null && poPreFilter.Data != null){
					var json = Encoding.UTF8.GetString(poPreFilter.Data);
					R.PreFilter = JSON.parse<PreFilter>(json);
				}
				R.PoPreFilter = poPreFilter;
			}

			// Get WeightArg
			if(!studyPlan.WeightArgId.IsNullOrDefault()){
				var poWeightArg = await fnWeightArg(studyPlan.WeightArgId, Ct);
				R.PoWeightArg = poWeightArg;
				if(poWeightArg != null && poWeightArg.Data != null){
					var json = Encoding.UTF8.GetString(poWeightArg.Data);
					var dict = ToolJson.JsonStrToDict(json);
					R.WeightArg = new JsonNode(dict);
				}
			}

			// Get WeightCalculator
			if(!studyPlan.WeightCalculatorId.IsNullOrDefault()){
				R.PoWeightCalculator = await fnWeightCalculator(studyPlan.WeightCalculatorId, Ct);
				// TODO: Create WeightCalctr from PoWeightCalculator if needed
			}

			return R;
		};
	}

	#endif
}
