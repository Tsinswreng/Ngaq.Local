namespace Ngaq.Local.Db.TswG;
using Ngaq.Local.Db;
using Tsinswreng.CsSqlHelper;

public class DbFnCtxMkr<TDbFnCtx>
	:BaseDbFnCtxMkr<TDbFnCtx>
	where TDbFnCtx: IBaseDbFnCtx, new()
{
	public DbFnCtxMkr(I_GetTxnAsy GetTxn):base(GetTxn){}
}
