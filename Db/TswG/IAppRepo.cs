namespace Ngaq.Local.Db.TswG;
using Tsinswreng.CsSqlHelper;


public partial interface IAppRepo<
	TEntity
	,TId
>
	:IRepo<TEntity, TId>
	where TEntity : class, new()
{


	/// 實體ˇ改後 手動調此方法
	[Obsolete]
	public Task<Func<
		TId
		,CT
		,Task<nil>
	>> FnUpd_BizUpdatedAt(IDbFnCtx Ctx, CT Ct);
	
	public Task<nil> BatUpd_BizUpdatedAt(
		IDbFnCtx Ctx, IAsyncEnumerable<TId> Ids, CT Ct
	);

}
