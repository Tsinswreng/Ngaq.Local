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

	public Task<IAsyncEnumerable<PoKv?>> BatGetByOwnerEtKStr(
		IAsyncEnumerable<(IdUser, str)> Owner_Key, CT Ct
	) {
		return SqlCmdMkr.RunInTxn(Ct, (Ctx)=>{
			return DaoKv.BatGetByOwnerEtKStr(Ctx, Owner_Key, Ct);
		});
	}

	public Task<IAsyncEnumerable<PoKv?>> BatGetByOwnerEtKI64(
		IAsyncEnumerable<(IdUser, i64)> Owner_Key
		,CT Ct
	){
		return SqlCmdMkr.RunInTxn(Ct, (Ctx)=>{
			return DaoKv.BatGetByOwnerEtKI64(Ctx, Owner_Key, Ct);
		});
	}
	

	public async Task<nil> BatSet(IAsyncEnumerable<PoKv> Kvs, CT Ct) {
		return SqlCmdMkr.RunInTxn(Ct, (Ctx)=>{
			return BatSet(Ctx, Kvs, Ct);
		});
	}
	public async Task<nil> BatSet(
		IDbFnCtx Ctx
		,IAsyncEnumerable<PoKv> Kvs, CT Ct
	){
		await RepoKv.BatUpdById(Ctx, Kvs, Ct);
		return NIL;
	}
	//public const str PathSep = "/";

}
