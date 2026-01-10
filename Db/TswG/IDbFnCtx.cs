namespace Ngaq.Local.Db.TswG;
using Tsinswreng.CsSqlHelper;


// public partial interface IDbFnCtx
// 	: Tsinswreng.CsSqlHelper.IDbFnCtx
// {

// }

// public partial class DbFnCtx : Tsinswreng.CsSqlHelper.DbFnCtx, IDbFnCtx{

// }


public partial interface ITxnDbFnCtxMkr{
	Task<IDbFnCtx> MkDbFnCtxAsy(CT Ct);
}

public partial class TxnDbFnCtxMkr(
	IMkrTxn GetTxn
)
	: ITxnDbFnCtxMkr
{
	public async Task<IDbFnCtx> MkDbFnCtxAsy(CT Ct){
		var R = new DbFnCtx(){
		};
		R.Txn = await GetTxn.MkTxnAsy(R, Ct);
		return R;
	}
}

