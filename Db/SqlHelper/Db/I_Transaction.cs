namespace Ngaq.Core.Infra.Db;

public interface I_TxnAsy : IDisposable{
	public object? RawTxn{get;}
	public Task<nil> BeginAsy(CancellationToken Ct);
	public Task<nil> CommitAsy(CancellationToken Ct);
	public Task<nil> RollbackAsy(CancellationToken Ct);
}
