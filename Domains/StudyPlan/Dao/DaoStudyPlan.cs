namespace Ngaq.Local.Domains.StudyPlan.Dao;

using Ngaq.Core.Shared.Base.Models.Po;
using Ngaq.Core.Shared.StudyPlan.Models;
using Ngaq.Core.Shared.StudyPlan.Models.Po.StudyPlan;
using Ngaq.Core.Shared.StudyPlan.Models.Po.WeightArg;
using Ngaq.Core.Shared.StudyPlan.Models.Po.WeightCalculator;
using Ngaq.Local.Db.TswG;
using Tsinswreng.CsCore;
using Tsinswreng.CsPage;
using Tsinswreng.CsSqlHelper;
using Tsinswreng.CsTools;
using IStr_Any = System.Collections.Generic.IDictionary<string, object?>;
using Str_Any = System.Collections.Generic.Dictionary<string, object?>;

public partial class DaoStudyPlan(
	ISqlCmdMkr SqlCmdMkr,
	ITblMgr TblMgr,
	IRepo<PoWeightArg, IdWeightArg> RepoWeightArg
) {
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
	
	
	
	
	


}
