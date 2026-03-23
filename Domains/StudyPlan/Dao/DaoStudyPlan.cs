namespace Ngaq.Local.Domains.StudyPlan.Dao;

using Ngaq.Core.Shared.Base.Models.Po;
using Ngaq.Core.Shared.StudyPlan.Models;
using Ngaq.Core.Shared.StudyPlan.Models.Po.PreFilter;
using Ngaq.Core.Shared.StudyPlan.Models.Po.StudyPlan;
using Ngaq.Core.Shared.StudyPlan.Models.Po.WeightArg;
using Ngaq.Core.Shared.StudyPlan.Models.Po.WeightCalculator;
using Ngaq.Core.Shared.User.Models.Po.User;
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
	
	public IAsyncEnumerable<JnStudyPlan?> BatGetStudyPlanById(
		IDbFnCtx Ctx, IAsyncEnumerable<IdStudyPlan> Ids, CT Ct
	){
		var R = RepoStudyPlan.BatGetAggById<JnStudyPlan>(Ctx, Ids, Ct);
		return R;
	}
	
	
	public class ReqPagePreFilter{
		public IdUser Owner{get;set;}
		public IPageQry PageQry{get;set;}
		
	}
	
	public async Task<IPageAsyE<PoPreFilter>> PagePreFilter(
		IDbFnCtx Ctx, ReqPagePreFilter Req
		,CT Ct
	){
		var Sql = TP.SqlSplicer().Select("*").From().Where1()
		.AndEq(x=>x.Owner, x=>x.One(Req.Owner))
		.LimOfst(Req.PageQry)
		;
		var r = SqlCmdMkr.RunDupliSql(Ctx, Sql, Ct);
		Req.PageQry.ToPageAsyE(r);
		throw new NotImplementedException();
		
		
	}
	
	
	
	
	
}
