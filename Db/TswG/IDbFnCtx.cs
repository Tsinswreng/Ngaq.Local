namespace Ngaq.Local.Db.TswG;
using Tsinswreng.CsSqlHelper;


public partial interface IDbFnCtx
	:IBaseDbFnCtx
{

}

public partial class DbFnCtx : BaseDbFnCtx, IDbFnCtx{

}


public  partial interface ITxnDbFnCtxMkr{
	Task<IDbFnCtx> MkDbFnCtxAsy(CT Ct);
}

public  partial class TxnDbFnCtxMkr(
	I_GetTxnAsy GetTxn
)
	: ITxnDbFnCtxMkr
{
	public async Task<IDbFnCtx> MkDbFnCtxAsy(CT Ct){
		var Txn = await GetTxn.GetTxnAsy(Ct);
		var R = new DbFnCtx(){
			Txn=Txn
		};
		return R;
	}
}

