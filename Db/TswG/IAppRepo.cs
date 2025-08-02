using Tsinswreng.CsSqlHelper;

namespace Ngaq.Local.Db.TswG;
public partial interface IAppRepo<
	TEntity
	,TId
>
	:IRepo<TEntity, TId>
	where TEntity : class, new()
{


	/// <summary>
	/// 實體ˇ改後 手動調此方法
	/// </summary>
	/// <param name="Ctx"></param>
	/// <param name="Ct"></param>
	/// <returns></returns>
	public Task<Func<
		TId
		,CT
		,Task<nil>
	>> FnUpd_UpdatedAt(IDbFnCtx Ctx, CT Ct);

}
