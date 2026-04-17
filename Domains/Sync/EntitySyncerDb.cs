namespace Ngaq.Local.Domains.Sync;

using Ngaq.Core.Infra;
using Ngaq.Core.Infra.Errors;
using Ngaq.Core.Shared.Base.Models.Po;
using Ngaq.Core.Shared.Sync;
using Tsinswreng.CsErr;
using Tsinswreng.CsSql;
using Tsinswreng.CsTools;

/// 通用 Po 同步器（實際操作數據庫）。
/// 規則：
/// 1) 按 Id 對位查庫（含軟刪）；
/// 2) Local 不存在則 Add；
/// 3) Local 存在則按 BizTime 比較，Remote 更新才 Upd；
/// 4) 逐條返回差異 DTO。
public class EntitySyncerDb<TPo, TId>:IEntitySyncerDb<TPo, TId>
	where TPo: class, IPoBase, IBizCreateUpdateTime, I_Id<TId>, new()
{
	IRepo<TPo, TId> Repo{get;}
	u64 BatchSize{get;}
	IEntitySyncerInMem<TPo> InMemSyncer{get;} = new EntitySyncerInMem<TPo>();

	/// <param name="Repo">通用倉儲，直接用於查/增/改。</param>
	/// <param name="BatchSize">攢批大小；不傳則用 BatchCollector 默認值。</param>
	public EntitySyncerDb(
		IRepo<TPo, TId> Repo,
		u64? BatchSize = null
	){
		this.Repo = Repo;
		this.BatchSize = BatchSize ?? BatchCollector<TPo, IList<DtoEntityDiffEtSync<TPo>>>.DfltBatchSize;
	}

	/// 批量同步並落庫，返回逐條差異結果。
	public async IAsyncEnumerable<DtoEntityDiffEtSync<TPo>> BatSyncPo(
		IDbFnCtx Ctx,
		IAsyncEnumerable<TPo> Pos,
		[System.Runtime.CompilerServices.EnumeratorCancellation]
		CT Ct
	){
		// step 1: 用 BatchCollector 避免逐條 I/O。
		await using var batch = new BatchCollector<TPo, IList<DtoEntityDiffEtSync<TPo>>>(
			FnAsy: async(rows, ct)=>await SyncOneBatch(Ctx, rows, ct),
			BatchSize: BatchSize
		);
		var batchRet = batch.AddToEnd(Pos, Ct);

		// step 2: 展平成最終逐條 DTO 輸出流。
		await foreach(var oneBatch in batchRet.WithCancellation(Ct)){
			foreach(var dto in oneBatch){
				yield return dto;
			}
		}
	}

	/// 同步一批：查本地 -> 比較 -> 分流 Add/Upd -> 返回差異列表。
	async Task<IList<DtoEntityDiffEtSync<TPo>>> SyncOneBatch(
		IDbFnCtx Ctx,
		IList<TPo> Rows,
		CT Ct
	){
		if(Rows.Count == 0){
			return [];
		}

		// step 1: 直接使用強類型 Id 查庫。
		var ids = Rows.Select(x=>x.Id);
		var locals = new List<TPo?>();
		await foreach(var local in Repo.BatGetByIdWithDel(Ctx, ToolAsyE.ToAsyE(ids), Ct).WithCancellation(Ct)){
			locals.Add(local);
		}
		if(locals.Count != Rows.Count){
			throw KeysErr.Sync.BatchGetByIdCountMismatch.ToErr();
		}

		// step 2: 按規則拆分 Add / Upd 並構造逐條結果。
		var toAdd = new List<TPo>();
		var toUpd = new List<TPo>();
		var ans = new List<DtoEntityDiffEtSync<TPo>>(Rows.Count);
		for(var i = 0; i < Rows.Count; i++){
			var remote = Rows[i];
			var local = locals[i];
			if(local is null){
				toAdd.Add(remote);
				ans.Add(new DtoEntityDiffEtSync<TPo>{
					LocalCompareToRemote = -1,
					SyncedEntity = remote,
				});
				continue;
			}

			// 用強類型 Id 比較是否一致；不一致視為調用錯誤。
			if(!EqualityComparer<TId>.Default.Equals(local.Id, remote.Id)){
				throw KeysErr.Sync.SyncShouldUseSameId.ToErr();
			}

			var diff = InMemSyncer.DiffPoByTime(local, remote);
			if(diff < 0){
				toUpd.Add(remote);
			}
			ans.Add(new DtoEntityDiffEtSync<TPo>{
				LocalCompareToRemote = diff,
				SyncedEntity = diff < 0 ? remote : default,
			});
		}

		// step 3: 分別批量落庫。
		if(toAdd.Count > 0){
			await Repo.BatAdd(Ctx, ToolAsyE.ToAsyE(toAdd), Ct);
		}
		if(toUpd.Count > 0){
			await Repo.BatUpd(Ctx, ToolAsyE.ToAsyE(toUpd), Ct);
		}
		return ans;
	}

}
