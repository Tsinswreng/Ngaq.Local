using System.Data;
using Ngaq.Core.Infra.Db;
using Ngaq.Local.Db;
using Tsinswreng.SqlHelper;
using Tsinswreng.SqlHelper.Cmd;

namespace Ngaq.Db;
public class SqlTxnRunner(
	IDbConnection DbConnection
)
	:IRunInTxn
	,ITxnRunner
{

	public async Task<T_Ret> RunInTxnAsy<T_Ret>(
		Func<
			CancellationToken, Task<T_Ret>
		> FnAsy
		,CancellationToken ct
	){
		using var Tx = DbConnection.BeginTransaction(IsolationLevel.Serializable);
		try{
			var ans = await FnAsy(ct);

			Tx.Commit();
			return ans;
		}
		catch (System.Exception){
			Tx.Rollback();
			throw;
		}
	}

	public async Task<T_Ret> RunTxnAsy<T_Ret>(
		ITxnAsy Txn
		,Func<
			CancellationToken, Task<T_Ret>
		> FnAsy
		,CancellationToken ct
	){
		try{
			await Txn.BeginAsy(ct);
			T_Ret ans = await FnAsy(ct);
			await Txn.CommitAsy(ct);
			return ans;
		}
		catch (System.Exception){
			await Txn.RollbackAsy(ct);
			throw;
		}

		// using var Tx = DbConnection.BeginTransaction(IsolationLevel.Serializable);
		// var AdoTx = new AdoTxn(Tx);
		// //var Ctx = new DbFnCtx{Txn = AdoTx};
		// DbFnCtx.Txn = AdoTx;
		// try{
		// 	var ans = await FnAsy(DbFnCtx, ct);
		// 	Tx.Commit();
		// 	return ans;
		// }
		// catch (System.Exception){
		// 	Tx.Rollback();
		// 	throw;
		// }
	}
}
