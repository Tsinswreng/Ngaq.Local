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
	ITable<PoStudyPlan> TS = TblMgr.GetTbl<PoStudyPlan>();
	ITable<PoPreFilter> TP = TblMgr.GetTbl<PoPreFilter>();
	/// 更新权重参数后触发业务更新时间
	
	
	public async Task<IAsyncEnumerable<JnStudyPlan?>> BatGetStudyPlanById(
		IDbFnCtx Ctx, IAsyncEnumerable<IdStudyPlan> Ids, CT Ct
	){
		var R = await RepoStudyPlan.BatSlctAggById<JnStudyPlan>(Ctx, Ids, Ct);
		return R;
	}
}
