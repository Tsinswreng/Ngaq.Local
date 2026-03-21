namespace Ngaq.Local.Domains.Kv.Svc;

using Ngaq.Core.Shared.Kv.Models;
using Ngaq.Core.Shared.Kv.Svc;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Sys.Models;
using Ngaq.Local.Db.TswG;
using Ngaq.Local.Domains.Kv.Dao;
using Tsinswreng.CsCore;
using Tsinswreng.CsSql;

using Z = SvcKv;
public partial class SvcKv(
	DaoKv DaoKv
	,TxnWrapper TxnWrapper
	,IRepo<PoKv, IdKv> RepoKv
	,ISqlCmdMkr SqlCmdMkr
)
	:ISvcKv
{
	DaoKv DaoKv = DaoKv;
	IRepo<PoKv, IdKv> RepoKv = RepoKv;
	TxnWrapper TxnWrapper = TxnWrapper;

	public Task<IAsyncEnumerable<PoKv?>> BatGetByOwnerEtKStr(
		IDbFnCtx? Ctx, IAsyncEnumerable<(IdUser, str)> Owner_Key, CT Ct
	) {
		return SqlCmdMkr.RunInTxnIfNoCtx(Ctx, Ct, (Ctx)=>{
			return DaoKv.BatGetByOwnerEtKStr(Ctx, Owner_Key, Ct);
		});
	}

	public Task<IAsyncEnumerable<PoKv?>> BatGetByOwnerEtKI64(
		IDbFnCtx? Ctx, IAsyncEnumerable<(IdUser, i64)> Owner_Key
		,CT Ct
	){
		return SqlCmdMkr.RunInTxnIfNoCtx(Ctx, Ct, (Ctx)=>{
			return DaoKv.BatGetByOwnerEtKI64(Ctx, Owner_Key, Ct);
		});
	}
	
	public async Task<nil> BatSet(
		IDbFnCtx? Ctx
		,IAsyncEnumerable<PoKv> Kvs, CT Ct
	){
		return await SqlCmdMkr.RunInTxnIfNoCtx(Ctx, Ct, (Ctx)=>{
			return RepoKv.BatUpdById(Ctx, Kvs, Ct);
		});
	}
}
