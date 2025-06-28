using Tsinswreng.CsSqlHelper;

namespace Ngaq.Local.Db;

public interface IDbFnCtx
	:IBaseDbFnCtx
{

}

public class DbFnCtx : BaseDbFnCtx, IDbFnCtx{

}


public interface ITxnDbFnCtxMkr{
	Task<IDbFnCtx> MkDbFnCtxAsy(CT Ct);
}

public class TxnDbFnCtxMkr(
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

