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

namespace Ngaq.Local.Domains.StudyPlan.Svc;

public partial class SvcStudyPlan:ISvcStudyPlan{
	
	ISvcKv SvcKv;
	DaoStudyPlan DaoStudyPlan;
	ISqlCmdMkr SqlCmdMkr;
	TxnWrapper TxnWrapper;
	IRepo<PoStudyPlan, IdStudyPlan> RepoStudyPlan;
	IRepo<PoWeightArg, IdWeightArg> RepoWeightArg;
	IRepo<PoWeightCalculator, IdWeightCalculator> RepoWeightCalculator;
	IRepo<PoPreFilter, IdPreFilter> RepoPreFilter;
	public SvcStudyPlan(
		ISvcKv SvcKv
		,DaoStudyPlan DaoStudyPlan
		,ISqlCmdMkr SqlCmdMkr
		,TxnWrapper TxnWrapper
		,IRepo<PoStudyPlan, IdStudyPlan> RepoStudyPlan
		,IRepo<PoWeightArg, IdWeightArg> RepoWeightArg
		,IRepo<PoWeightCalculator, IdWeightCalculator> RepoWeightCalculator
		,IRepo<PoPreFilter, IdPreFilter> RepoPreFilter
	){
		this.SvcKv = SvcKv;
		this.DaoStudyPlan = DaoStudyPlan;
		this. SqlCmdMkr = SqlCmdMkr;
		this.TxnWrapper = TxnWrapper;
		this.RepoStudyPlan = RepoStudyPlan;
		this.RepoWeightArg = RepoWeightArg;
		this.RepoWeightCalculator = RepoWeightCalculator;
		this.RepoPreFilter = RepoPreFilter;
	}

	

	public async Task<IdStudyPlan?> GetCurStudyPlanId(
		IDbFnCtx? Ctx
		,IUserCtx User
		,CT Ct
	){
		return await TxnWrapper.Wrap(FnGetCurStudyPlanId, User, Ct);
	}

	public async Task<nil> SetCurStudyPlanId(
		IDbFnCtx? Ctx, IUserCtx User, IdStudyPlan StudyPlanId, CT Ct
	){
		return await SqlCmdMkr.RunInTxnIfNoCtx(Ctx, Ct, async(Ctx)=>{
			var kv = new PoKv();
			kv.Owner = User.UserId;
			kv.SetStrStr(KeysClientKv.CurStudyPlanId, StudyPlanId+"");
			return await SvcKv.BatSet(Ctx, ToolAsyE.ToAsyE([kv]), Ct);
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
		IDbFnCtx? Ctx, IUserCtx User
		, IAsyncEnumerable<PoPreFilter> Pos
		,CT Ct
	){
		return await SqlCmdMkr.RunInTxnIfNoCtx(Ctx, Ct, async(Ctx)=>{
			Pos = EnsureOwner(User.UserId, Pos);
			await RepoPreFilter.BatAdd(Ctx, Pos, Ct);
			return NIL;
		});
	}
	
	public async Task<nil> BatAddWeightArg(
		IDbFnCtx? Ctx, IUserCtx User
		, IAsyncEnumerable<PoWeightArg> Pos
		,CT Ct
	){
		return await SqlCmdMkr.RunInTxnIfNoCtx(Ctx, Ct, async(Ctx)=>{
			Pos = EnsureOwner(User.UserId, Pos);
			await RepoWeightArg.BatAdd(Ctx, Pos, Ct);
			return NIL;
		});
	}
	
	public async Task<nil> BatAddWeightCalculator(
		IDbFnCtx? Ctx, IUserCtx User
		, IAsyncEnumerable<PoWeightCalculator> Pos
		,CT Ct
	){
		return await SqlCmdMkr.RunInTxnIfNoCtx(Ctx, Ct, async(Ctx)=>{
			Pos = EnsureOwner(User.UserId, Pos);
			await RepoWeightCalculator.BatAdd(Ctx, Pos, Ct);
			return NIL;
		});
	}

	public async Task<IPageAsyE<PoStudyPlan>> PageStudyPlan(
		IDbFnCtx? Ctx
		,ReqPageStudyPlan Req
		,CT Ct
	){
		Ctx ??= new DbFnCtx();
		return await DaoStudyPlan.PageStudyPlan(Ctx, Req, Ct);
	}

	public async Task<IPageAsyE<PoPreFilter>> PagePreFilter(
		IDbFnCtx? Ctx
		,ReqPagePreFilter Req
		,CT Ct
	){
		Ctx ??= new DbFnCtx();
		return await DaoStudyPlan.PagePreFilter(Ctx, Req, Ct);
	}

	public async Task<IPageAsyE<PoWeightArg>> PageWeightArg(
		IDbFnCtx? Ctx
		,ReqPageWeightArg Req
		,CT Ct
	){
		Ctx ??= new DbFnCtx();
		return await DaoStudyPlan.PageWeightArg(Ctx, Req, Ct);
	}

	public async Task<IPageAsyE<PoWeightCalculator>> PageWeightCalculator(
		IDbFnCtx? Ctx
		,ReqPageWeightCalculator Req
		,CT Ct
	){
		Ctx ??= new DbFnCtx();
		return await DaoStudyPlan.PageWeightCalculator(Ctx, Req, Ct);
	}

	
	[Obsolete]
	public async Task<Func<
		IUserCtx, IdStudyPlan
		,CT,Task<nil>
	>> FnSetCurStudyPlanId(
		IDbFnCtx Ctx, CT Ct
	){
		var fnSet = await SvcKv.FnSet(Ctx, Ct);
		return async (UserCtx, StudyPlanId, Ct)=>{
			var kv = new PoKv();
			kv.Owner = UserCtx.UserId;
			kv.SetStrStr(KeysClientKv.CurStudyPlanId, StudyPlanId+"");
			await fnSet(kv, Ct);
			return NIL;
		};
	}
	
	[Obsolete]
	public async Task<Func<
		IUserCtx,
		CT, Task<IdStudyPlan?>
	>> FnGetCurStudyPlanId(IDbFnCtx Ctx, CT Ct){
		var fnGet = await SvcKv.FnGetByOwnerEtKey(Ctx, Ct);
		return async (UserCtx, Ct)=>{
			var kv = await fnGet(UserCtx.UserId, KeysClientKv.CurStudyPlanId, Ct);
			if(kv == null || str.IsNullOrEmpty(kv.VStr)){
				return null;
			}
			return IdStudyPlan.FromLow64Base(kv.VStr);
		};
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
