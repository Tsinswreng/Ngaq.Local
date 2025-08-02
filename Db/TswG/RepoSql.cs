namespace Ngaq.Local.Db.TswG;

using Tsinswreng.CsSqlHelper;
using Tsinswreng.CsDictMapper;
using Ngaq.Core.Model.Po;
using Ngaq.Core.Infra;
using Tsinswreng.CsCore;

public partial class AppRepo<
	TEntity
	, TId
>
	:SqlRepo<TEntity, TId>
	,IAppRepo<TEntity, TId>
	where TEntity : class, new()
{
	public AppRepo(ITblMgr TblMgr, ISqlCmdMkr SqlCmdMkr, IDictMapperShallow DictMapper) : base(TblMgr, SqlCmdMkr, DictMapper) {
	}


	[Impl]
	public async Task<Func<
		TId
		,CT
		,Task<nil>
	>> FnAfterUpd(IDbFnCtx Ctx, CT Ct){
		var T = TblMgr.GetTable<TEntity>();
		var NDbUpdatedAt = nameof(IPoBase.DbUpdatedAt);
		var Upd_DbUpdatedAtById = await FnUpdOneColById(Ctx, NDbUpdatedAt, Ct);
		var Fn = async(TId Id, CT Ct)=>{
			var Now = Tempus.Now();
			var Raw = T.UpperToRaw(Now, NDbUpdatedAt);
			await Upd_DbUpdatedAtById(Id, Raw, Ct);
			return NIL;
		};
		return Fn;
	}


}
