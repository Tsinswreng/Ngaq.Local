namespace Ngaq.Local.Db.TswG;

using Tsinswreng.CsSqlHelper;
using Tsinswreng.CsDictMapper;
using Ngaq.Core.Infra;
using Tsinswreng.CsCore;
using Ngaq.Core.Models.Po;

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
	>> FnUpd_UpdatedAt(IDbFnCtx Ctx, CT Ct){
		var T = TblMgr.GetTbl<TEntity>();
		var NUpdatedAt = nameof(IPoBase.UpdatedAt);
		var Upd_DbUpdatedAtById = await FnUpdOneColById(Ctx, NUpdatedAt, Ct);
		var Fn = async(TId Id, CT Ct)=>{
			var Now = Tempus.Now();
			var Raw = T.UpperToRaw(Now, NUpdatedAt);
			await Upd_DbUpdatedAtById(Id, Raw, Ct);
			return NIL;
		};
		return Fn;
	}


}
