namespace Ngaq.Local.Domains.Sync;

using Ngaq.Core.Infra;
using Ngaq.Core.Shared.Sync;

/// 通用數據庫同步器。
/// 調用方可把“具體實體如何查庫與落庫”的邏輯以委託注入，
/// 這樣同一個同步器可覆用到不同領域實體。
public class EntitySyncerDb<T>(
	Func<IDbUserCtx, IAsyncEnumerable<T>, CT, IAsyncEnumerable<DtoEntityDiffEtSync<T>>> FnBatSyncPo
):IEntitySyncerDb<T>{

	Func<IDbUserCtx, IAsyncEnumerable<T>, CT, IAsyncEnumerable<DtoEntityDiffEtSync<T>>> FnBatSyncPo{get;}
		= FnBatSyncPo;

	/// 批量同步實體，實際數據庫細節由構造函數注入的委託決定。
	public IAsyncEnumerable<DtoEntityDiffEtSync<T>> BatSyncPo(
		IDbUserCtx Ctx,
		IAsyncEnumerable<T> Pos,
		CT Ct
	){
		return FnBatSyncPo(Ctx, Pos, Ct);
	}
}
