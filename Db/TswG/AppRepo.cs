namespace Ngaq.Local.Db.TswG;

using Tsinswreng.CsSql;
using Ngaq.Core.Infra;
using Tsinswreng.CsCore;
using Ngaq.Core.Shared.Base.Models.Po;
using Tsinswreng.CsStrAcc;

public partial class AppRepo<
	TEntity
	, TId
>
	:SqlRepo<TEntity, TId>
	,IRepo<TEntity, TId>
	,IAppRepo<TEntity, TId>
	where TEntity : class, new()
{
	public AppRepo(ITblMgr TblMgr, ISqlCmdMkr SqlCmdMkr, IPropAccessorMgr DictMapper) : base(TblMgr, SqlCmdMkr, DictMapper) {

	}


	[Impl]
	public async Task<Func<
		TId
		,CT
		,Task<nil>
	>> FnUpd_BizUpdatedAt(IDbFnCtx? Ctx, CT Ct){
		var T = TblMgr.GetTbl<TEntity>();
		var NUpdatedAt = nameof(IBizCreateUpdateTime.BizUpdatedAt);
		var Upd_DbUpdatedAtById = await FnUpdOneColById(Ctx, NUpdatedAt, Ct);
		return async(Id, Ct)=>{
			var Now = Tempus.Now();
			var Raw = T.UpperToRaw(Now, NUpdatedAt);
			await Upd_DbUpdatedAtById(Id, Raw, Ct);
			return NIL;
		};
	}
	
	[Impl]
	public async Task<nil> BatBizTouch(
		IDbFnCtx Ctx, IAsyncEnumerable<TId> Ids, CT Ct
	){
		var Now = Tempus.Now();
		var CodeDicts = Ids.Select(x=>new Dictionary<str, obj?>(){
			[nameof(IBizCreateUpdateTime.BizUpdatedAt)] = Now
		});
		await BatUpdByCodeDict(Ctx, Ids, CodeDicts, Ct);
		return NIL;
	}
}

public static class ExtnIRepo{
	extension<TPo, TId>(IRepo<TPo, TId> z)
		where TPo : class, new()
	{
		public IAppRepo<TPo, TId> AsAppRepo(){
			if(z is not IAppRepo<TPo, TId> R){
				throw new InvalidCastException("Registered Repo is not IAppRepo;"
				+" You should Register <IRepo, AppRepo>"
				);
			}
			return R;
		}
	}
	
}
