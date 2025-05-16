using Ngaq.Core.Infra.Db;
using Ngaq.Local.Db;

namespace Ngaq.Db;
public class TxnAsyFnRunner(
	DbCtx DbCtx
) : I_TxnAsyFnRunner{

	public async Task<T_Ret> TxnAsy<T_Ret>(
		Func<CancellationToken, Task<T_Ret>> FnAsy
		,CancellationToken ct
	){
		using var Tx = await DbCtx.Database.BeginTransactionAsync(ct);
		try{
			var ans = await FnAsy(ct);
			await Tx.CommitAsync(ct);
			return ans;
		}
		catch (System.Exception){
			await Tx.RollbackAsync(ct);
			throw;
		}
	}
}
