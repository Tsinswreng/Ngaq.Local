using Ngaq.Core.Frontend.Kv;
using Ngaq.Core.Shared.Kv.Models;
using Ngaq.Core.Shared.Kv.Svc;
using Ngaq.Core.Shared.StudyPlan.Models;
using Ngaq.Core.Shared.StudyPlan.Models.Po.PreFilter;
using Ngaq.Core.Shared.StudyPlan.Models.Po.StudyPlan;
using Ngaq.Core.Shared.StudyPlan.Models.Po.WeightArg;
using Ngaq.Core.Shared.StudyPlan.Models.Po.WeightCalculator;
using Ngaq.Core.Shared.User.UserCtx;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Local.Db.TswG;
using Tsinswreng.CsSqlHelper;

namespace Ngaq.Local.Domains.StudyPlan.Svc;


public class SvcStudyPlan{

	ISvcKv SvcKv;
	TxnWrapper TxnWrapper;
	IAppRepo<PoStudyPlan, IdStudyPlan> RepoStudyPlan;
	IAppRepo<PoWeightArg, IdWeightArg> RepoWeightArg;
	IAppRepo<PoWeightCalculator, IdWeightCalculator> RepoWeightCalculator;
	IAppRepo<PoPreFilter, IdPreFilter> RepoPreFilter;
	public SvcStudyPlan(
		ISvcKv SvcKv
		,TxnWrapper TxnWrapper
		,IAppRepo<PoStudyPlan, IdStudyPlan> RepoStudyPlan
		,IAppRepo<PoWeightArg, IdWeightArg> RepoWeightArg
		,IAppRepo<PoWeightCalculator, IdWeightCalculator> RepoWeightCalculator
		,IAppRepo<PoPreFilter, IdPreFilter> RepoPreFilter
	){
		this.SvcKv = SvcKv;
		this.TxnWrapper = TxnWrapper;
		this.RepoStudyPlan = RepoStudyPlan;
		this.RepoWeightArg = RepoWeightArg;
		this.RepoWeightCalculator = RepoWeightCalculator;
		this.RepoPreFilter = RepoPreFilter;
	}


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

	public async Task<IdStudyPlan?> GetCurStudyPlanId(
		IUserCtx User
		,CT Ct
	){
		return await TxnWrapper.Wrap(FnGetCurStudyPlanId, User, Ct);
	}

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

	public async Task<nil> SetCurStudyPlanId(
		IUserCtx User
		,IdStudyPlan StudyPlanId
		,CT Ct
	){
		return await TxnWrapper.Wrap(FnSetCurStudyPlanId, User, StudyPlanId, Ct);
	}

	public async Task<Func<
		IUserCtx, IdStudyPlan
		,CT, Task<BoStudyPlan?>
	>> FnGetStudyPlanById(IDbFnCtx Ctx, CT Ct){
		var fnPreFilter = await RepoPreFilter.FnSlctOneById(Ctx, Ct);
		var fnWeightArg = await RepoWeightArg.FnSlctOneById(Ctx, Ct);
		var fnWeightCalculator = await RepoWeightCalculator.FnSlctOneById(Ctx, Ct);
		var fnStudyPlan = await RepoStudyPlan.FnSlctOneById(Ctx, Ct);

		return async (UserCtx, StudyPlanId, Ct)=>{
			throw new NotImplementedException();
			// var studyPlan = await fnStudyPlan(StudyPlanId, Ct);
			// if(studyPlan == null){
			// 	return null;
			// }
			// var R = new BoStudyPlan();
			// R.PreFilter = await fnPreFilter(studyPlan.PreFilterId, Ct);


		};
	}

}
