namespace Ngaq.Local.Db.TswG;

using Ngaq.Core.Infra;
using Ngaq.Core.Shared.Base.Models.Po;
using Tsinswreng.CsCore;
using Tsinswreng.CsSql;
using Tsinswreng.CsTempus;

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
	
	[Doc(@$"獲取 某指定時間後 有變更的實體。
	包含已被軟刪除的。 一般用于備份同步
	{nameof(PoBaseBizTime.DelAt)}或{nameof(PoBaseBizTime.BizUpdatedAt)}
	大於 {nameof(ChangedTime)} 都算有變更。
	")]
	public IAsyncEnumerable<TEntity> BatGetChangedEntityAfterTimeWithDel(
		IDbUserCtx Ctx, Tempus ChangedTime, CT Ct
	);

	/// 實體ˇ改後 手動調此方法
	[Obsolete]
	public Task<Func<
		TId
		,CT
		,Task<nil>
	>> FnUpd_BizUpdatedAt(IDbFnCtx Ctx, CT Ct);
	
	

}
