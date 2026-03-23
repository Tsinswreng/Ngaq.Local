namespace Ngaq.Local.Domains.StudyPlan.Svc;

using Ngaq.Core.Frontend.Kv;
using Ngaq.Core.Infra.IF;
using Ngaq.Core.Shared.Base.Models.Po;
using Ngaq.Core.Shared.Kv.Models;
using Ngaq.Core.Shared.Kv.Svc;
using Ngaq.Core.Shared.StudyPlan.Models;
using Ngaq.Core.Shared.StudyPlan.Models.Req;
using Ngaq.Core.Shared.StudyPlan.Models.Po.PreFilter;
using Ngaq.Core.Shared.StudyPlan.Models.Po.StudyPlan;
using Ngaq.Core.Shared.StudyPlan.Models.Po.WeightArg;
using Ngaq.Core.Shared.StudyPlan.Models.Po.WeightCalculator;
using Ngaq.Core.Shared.StudyPlan.Models.PreFilter;
using Ngaq.Core.Shared.StudyPlan.Svc;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.User.UserCtx;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Tools;
using Ngaq.Local.Db.TswG;
using System.Text;
using Tsinswreng.CsCore;
using Tsinswreng.CsPage;
using Tsinswreng.CsSql;
using Tsinswreng.CsTools;


public partial class SvcStudyPlan:ISvcStudyPlan{
	
	[Impl]
	public async Task<nil> BatAddPreFilter(
		IUserCtx User
		,IAsyncEnumerable<PoPreFilter> Pos
		,CT Ct
	){
		return SqlCmdMkr.RunInTxn(Ct, (Ctx)=>{
			return BatAddPreFilter(Ctx, User, Pos, Ct);
		});
	}
	
	[Impl]
	public Task<nil> BatAddWeightArg(
		IUserCtx User
		,IAsyncEnumerable<PoWeightArg> Pos
		,CT Ct
	){
		return SqlCmdMkr.RunInTxn(Ct, (Ctx)=>{
			return BatAddWeightArg(Ctx, User, Pos, Ct);
		});
	}
	
	[Impl]
	public Task<nil> BatAddWeightCalculator(
		IUserCtx User
		,IAsyncEnumerable<PoWeightCalculator> Pos
		,CT Ct
	){
		return SqlCmdMkr.RunInTxn(Ct, (Ctx)=>{
			return BatAddWeightCalculator(Ctx, User, Pos, Ct);
		});
	}

	[Impl]
	public Task<IPageAsyE<PoStudyPlan>> PageStudyPlan(
		ReqPageStudyPlan Req
		,CT Ct
	){
		return PageStudyPlan(null, Req, Ct);
	}

	[Impl]
	public Task<IPageAsyE<PoPreFilter>> PagePreFilter(
		ReqPagePreFilter Req
		,CT Ct
	){
		return PagePreFilter(null, Req, Ct);
	}

	[Impl]
	public Task<IPageAsyE<PoWeightArg>> PageWeightArg(
		ReqPageWeightArg Req
		,CT Ct
	){
		return PageWeightArg(null, Req, Ct);
	}

	[Impl]
	public Task<IPageAsyE<PoWeightCalculator>> PageWeightCalculator(
		ReqPageWeightCalculator Req
		,CT Ct
	){
		return PageWeightCalculator(null, Req, Ct);
	}
	
}
