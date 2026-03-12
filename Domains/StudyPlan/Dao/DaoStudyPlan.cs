namespace Ngaq.Local.Domains.StudyPlan.Dao;

using Ngaq.Core.Shared.Base.Models.Po;
using Ngaq.Core.Shared.StudyPlan.Models;
using Ngaq.Core.Shared.StudyPlan.Models.Po.PreFilter;
using Ngaq.Core.Shared.StudyPlan.Models.Po.StudyPlan;
using Ngaq.Core.Shared.StudyPlan.Models.Po.WeightArg;
using Ngaq.Core.Shared.StudyPlan.Models.Po.WeightCalculator;
using Ngaq.Local.Db.TswG;
using Tsinswreng.CsCore;
using Tsinswreng.CsPage;
using Tsinswreng.CsSql;
using Tsinswreng.CsTools;
using IStr_Any = System.Collections.Generic.IDictionary<string, object?>;
using Str_Any = System.Collections.Generic.Dictionary<string, object?>;

public partial class DaoStudyPlan(
	ISqlCmdMkr SqlCmdMkr
	,ITblMgr TblMgr
	,IRepo<PoStudyPlan, IdStudyPlan> RepoStudyPlan
	,IRepo<PoWeightArg, IdWeightArg> RepoWeightArg
	,IRepo<PoWeightCalculator, IdWeightCalculator> RepoWeightCalculator
	,IRepo<PoPreFilter, IdPreFilter> RepoPreFilter
) {
	ITable<PoStudyPlan> TblStudyPlan = TblMgr.GetTbl<PoStudyPlan>();
	ITable<PoPreFilter> TblPreFilter = TblMgr.GetTbl<PoPreFilter>();
	/// 更新权重参数后触发业务更新时间
	public async Task<Func<
		IdWeightArg,
		CT,
		Task<nil>
	>> FnTriggerOnRootAfterUpd(IDbFnCtx Ctx, CT Ct) {
		var UpdPoWeightArg = await RepoWeightArg.AsAppRepo().FnUpd_BizUpdatedAt(Ctx, Ct);
		return async (WeightArgId, Ct) => {
			await UpdPoWeightArg(WeightArgId, Ct);
			return NIL;
		};
	}
	
	// public async Task<IAsyncEnumerable<BoStudyPlan?>> BatGetStudyPlanById(
	// 	IDbFnCtx Ctx, IEnumerable<IdStudyPlan> Ids, CT Ct
	// ){
	// 	var poStudyPlans = await RepoStudyPlan.BatSlctById(Ctx, Ids, Ct);
		
	// 	var batch = BatchCollector<PoStudyPlan?, BoStudyPlan>.Mk((poStudyPlan, Ct)=>{
			
	// 		RepoStudyPlan.IncludeEntitysByKeys(
	// 			Ctx, nameof(PoStudyPlan.PreFilterId), null, poStudyPlans.Select(x=>x?.PreFilterId), x=>x.PreFilterId, TblPreFilter, Ct
	// 		);
	// 		poStudyPlan
	// 	});
		
	// }
	
	


}
