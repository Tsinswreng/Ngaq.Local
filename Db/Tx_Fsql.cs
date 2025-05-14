// using Ngaq.Core.Infra.Db;

// namespace Ngaq.Local.Db;

// public class Tx_Fsql : I_Transaction {
// 	FreeSql.IRepositoryUnitOfWork UnitOfWork{get;set;}
// 	public IFreeSql Fsql{get;set;}
// 	public Tx_Fsql(IFreeSql Fsql){
// 		this.Fsql = Fsql;
// 	}

// 	public async Task<nil> BeginAsy() {
// 		UnitOfWork = Fsql.CreateUnitOfWork();
// 		return Nil;
// 	}

// 	public async Task<nil> CommitAsy() {
// 		UnitOfWork.Commit();
// 		return Nil;
// 	}

// 	public void Dispose() {
// 		UnitOfWork.Dispose();
// 	}

// 	public async Task<nil> RollbackAsy() {
// 		UnitOfWork.Rollback();
// 		return Nil;
// 	}
// }
