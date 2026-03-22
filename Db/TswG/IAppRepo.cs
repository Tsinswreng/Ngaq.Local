namespace Ngaq.Local.Db.TswG;

using Ngaq.Core.Shared.Base.Models.Po;
using Tsinswreng.CsCore;
using Tsinswreng.CsSql;


public partial interface IAppRepo<
	TEntity
	,TId
>
	:IRepo<TEntity, TId>
	where TEntity : class, new()
{

	[Doc(@$"更新實體的{nameof(PoBaseBizTime.BizUpdatedAt)}")]
	public Task<nil> BatBizTouch(
		IDbFnCtx Ctx, IAsyncEnumerable<TId> Ids, CT Ct
	);

	/// 實體ˇ改後 手動調此方法
	[Obsolete]
	public Task<Func<
		TId
		,CT
		,Task<nil>
	>> FnUpd_BizUpdatedAt(IDbFnCtx Ctx, CT Ct);

}
