namespace Ngaq.Local.Domains.StudyPlan.Dao;

using Ngaq.Core.Model.Po;
using Ngaq.Core.Shared.Base.Models.Po;
using Ngaq.Core.Shared.StudyPlan.Models.Po.WeightArg;
using Ngaq.Core.Shared.StudyPlan.Models.Po.WeightCalculator;
using Ngaq.Local.Db.TswG;
using Tsinswreng.CsCore;
using Tsinswreng.CsPage;
using Tsinswreng.CsSqlHelper;
using Tsinswreng.CsTools;
using IStr_Any = System.Collections.Generic.IDictionary<string, object?>;
using Str_Any = System.Collections.Generic.Dictionary<string, object?>;

public partial class DaoSqlWeightArgs(
	ISqlCmdMkr SqlCmdMkr,
	ITblMgr TblMgr,
	IAppRepo<PoWeightArg, IdWeightArg> RepoWeightArg
) {
	/// <summary>
	/// 更新权重参数后触发业务更新时间
	/// </summary>
	public async Task<Func<
		IdWeightArg,
		CT,
		Task<nil>
	>> FnTriggerOnRootAfterUpd(IDbFnCtx Ctx, CT Ct) {
		var UpdPoWeightArg = await RepoWeightArg.FnUpd_BizUpdatedAt(Ctx, Ct);
		return async (WeightArgId, Ct) => {
			await UpdPoWeightArg(WeightArgId, Ct);
			return NIL;
		};
	}

	/// <summary>
	/// 硬删除已软删除的权重参数记录
	/// </summary>
	async Task<Func<
		CT, Task<nil>
	>> FnHardDelSoftDeletedInWeightArgsDb(IDbFnCtx Ctx, ITable Tbl, CT Ct) {
		var T = Tbl;
		var Sql = $"""
            DELETE FROM {T.Qt(T.DbTblName)}
            WHERE {T.Fld(nameof(IPoBase.DelAt))} != 0
            """;
		var Cmd = await Ctx.PrepareToDispose(SqlCmdMkr, Sql, Ct);
		return async (Ct) => {
			await Cmd.All(Ct);
			return NIL;
		};
	}

	/// <summary>
	/// 分页数据转换为实体列表
	/// </summary>
	async Task<IList<TPo>> _PageToList<TPo>(
		IPage<IStr_Any> Page,
		ITable Tbl
	) where TPo : new() {
		if (Page.Data == null) {
			return new List<TPo>();
		}
		return Page.Data.Select(
			D => Tbl.AssignEntity(D, new TPo())
		).AsOrToList();
	}

	/// <summary>
	/// 将上层ID转换为原始数据库值
	/// </summary>
	public object? IdUpperToRaw<TPo>(object UpperId) {
		var T = TblMgr.GetTbl<TPo>();
		return T.UpperToRaw(UpperId, nameof(I_Id<nil>.Id));
	}
}
