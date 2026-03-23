namespace Ngaq.Local.Domains.StudyPlan.Dao;

using Ngaq.Core.Shared.Base.Models.Po;
using Ngaq.Core.Shared.StudyPlan.Models;
using Ngaq.Core.Shared.StudyPlan.Models.Req;
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
	ITable<PoWeightArg> TWA = TblMgr.GetTbl<PoWeightArg>();
	ITable<PoWeightCalculator> TWC = TblMgr.GetTbl<PoWeightCalculator>();
	/// 更新权重参数后触发业务更新时间
	
	public IAsyncEnumerable<JnStudyPlan?> BatGetStudyPlanById(
		IDbFnCtx Ctx, IAsyncEnumerable<IdStudyPlan> Ids, CT Ct
	){
		var R = RepoStudyPlan.BatGetAggById<JnStudyPlan>(Ctx, Ids, Ct);
		return R;
	}
	
	
	
	public async Task<IPageAsyE<PoPreFilter>> PagePreFilter(
		IDbFnCtx Ctx, ReqPagePreFilter Req
		,CT Ct
	){
		var Sql = TP.SqlSplicer().Select("*").From().Where1()
		.AndEq(x=>x.Owner, x=>x.One(Req.Owner));
		if(!string.IsNullOrEmpty(Req.UniqNameSearch)){
			Sql.And();
			Sql.Bool(x=>x.UniqName, "LIKE", x=>x.One("%"+Req.UniqNameSearch+"%"));
		}
		Sql.OrderBy([
			TP.QtCol(x=>x.BizUpdatedAt)+"Desc"
			,TP.QtCol(x=>x.Id)+"Desc"
		])
		.LimOfst(Req.PageQry)
		;
		var r = SqlCmdMkr.RunDupliSql(Ctx, TP, Sql, Ct);
		return Req.PageQry.ToPageAsyE(r);
	}

	public async Task<IPageAsyE<PoStudyPlan>> PageStudyPlan(
		IDbFnCtx Ctx, ReqPageStudyPlan Req
		,CT Ct
	){
		var Sql = TS.SqlSplicer().Select("*").From().Where1()
		.AndEq(x=>x.Owner, x=>x.One(Req.Owner));
		if(!string.IsNullOrEmpty(Req.UniqNameSearch)){
			Sql.And();
			Sql.Bool(x=>x.UniqName, "LIKE", x=>x.One("%"+Req.UniqNameSearch+"%"));
		}
		Sql.OrderBy([
			TS.QtCol(x=>x.BizUpdatedAt)+"Desc"
			,TS.QtCol(x=>x.Id)+"Desc"
		])
		.LimOfst(Req.PageQry)
		;
		var r = SqlCmdMkr.RunDupliSql(Ctx, TS, Sql, Ct);
		return Req.PageQry.ToPageAsyE(r);
	}

	public async Task<IPageAsyE<PoWeightArg>> PageWeightArg(
		IDbFnCtx Ctx, ReqPageWeightArg Req
		,CT Ct
	){
		var Sql = TWA.SqlSplicer().Select("*").From().Where1()
		.AndEq(x=>x.Owner, x=>x.One(Req.Owner));
		if(!string.IsNullOrEmpty(Req.UniqNameSearch)){
			Sql.And();
			Sql.Bool(x=>x.UniqName, "LIKE", x=>x.One("%"+Req.UniqNameSearch+"%"));
		}
		Sql.OrderBy([
			TWA.QtCol(x=>x.BizUpdatedAt)+"Desc"
			,TWA.QtCol(x=>x.Id)+"Desc"
		])
		.LimOfst(Req.PageQry)
		;
		var r = SqlCmdMkr.RunDupliSql(Ctx, TWA, Sql, Ct);
		return Req.PageQry.ToPageAsyE(r);
	}

	public async Task<IPageAsyE<PoWeightCalculator>> PageWeightCalculator(
		IDbFnCtx Ctx, ReqPageWeightCalculator Req
		,CT Ct
	){
		var Sql = TWC.SqlSplicer().Select("*").From().Where1()
		.AndEq(x=>x.Owner, x=>x.One(Req.Owner));
		if(!string.IsNullOrEmpty(Req.UniqNameSearch)){
			Sql.And();
			Sql.Bool(x=>x.UniqName, "LIKE", x=>x.One("%"+Req.UniqNameSearch+"%"));
		}
		Sql.OrderBy([
			TWC.QtCol(x=>x.Id)+"Desc"
		])
		.LimOfst(Req.PageQry)
		;
		var r = SqlCmdMkr.RunDupliSql(Ctx, TWC, Sql, Ct);
		return Req.PageQry.ToPageAsyE(r);
	}
}
