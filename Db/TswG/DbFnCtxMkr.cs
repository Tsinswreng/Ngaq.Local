namespace Ngaq.Local.Db.TswG;
using Ngaq.Local.Db;
using Tsinswreng.CsSqlHelper;

public partial class DbFnCtxMkr<TDbFnCtx>
	:MkrDbFnCtx<TDbFnCtx>
	where TDbFnCtx: Tsinswreng.CsSqlHelper.IDbFnCtx, new()
{
	public DbFnCtxMkr(IMkrTxn GetTxn):base(GetTxn){}
}
