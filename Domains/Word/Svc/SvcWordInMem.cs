namespace Ngaq.Local.Domains.Word.Svc;

using Ngaq.Core.Shared.Sync;
using Ngaq.Core.Shared.Word.Models;
using Ngaq.Core.Shared.Word.Models.Dto;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Shared.Word.Models.Po.Learn;
using Ngaq.Core.Shared.Word.Models.Po.Word;
using Ngaq.Core.Shared.Word.Svc;
using Ngaq.Core.Tools;

/// 單詞同步的純內存實現，不直接操作數據庫。
public class SvcWordInMem:ISvcWordInMem{

	/// 比較兩個屬性資產；僅當 Remote 更新時返回需要寫回的實體。
	public DtoEntityDiffEtSync<PoWordProp> SyncProp(PoWordProp Local, PoWordProp Remote){
		var syncer = new EntitySyncerInMem<PoWordProp>();
		var diff = syncer.DiffPoByTime(Local, Remote);
		return new DtoEntityDiffEtSync<PoWordProp>{
			LocalCompareToRemote = diff,
			SyncedEntity = diff < 0 ? Remote : null,
		};
	}

	/// 比較兩個學習記錄資產；僅當 Remote 更新時返回需要寫回的實體。
	public DtoEntityDiffEtSync<PoWordLearn> SyncLearn(PoWordLearn Local, PoWordLearn Remote){
		var syncer = new EntitySyncerInMem<PoWordLearn>();
		var diff = syncer.DiffPoByTime(Local, Remote);
		return new DtoEntityDiffEtSync<PoWordLearn>{
			LocalCompareToRemote = diff,
			SyncedEntity = diff < 0 ? Remote : null,
		};
	}

	/// 比較聚合根 PoWord；僅當 Remote 更新時返回需要寫回的實體。
	public DtoEntityDiffEtSync<PoWord> SyncPoWord(PoWord Local, PoWord Remote){
		if(Local.IsSameUserWord(Remote) == false){
			throw Ngaq.Core.Infra.Errors.ItemsErr.Word.__And__IsNotSameUserWord.ToErr(Local.Id, Remote.Id);
		}
		if(Local.Id != Remote.Id){
			var keep = Local.BizCreatedAt <= Remote.BizCreatedAt ? Local : Remote;
			return new DtoEntityDiffEtSync<PoWord>{
				LocalCompareToRemote = Local.BizCreatedAt.CompareTo(Remote.BizCreatedAt),
				SyncedEntity = keep,
			};
		}
		var syncer = new EntitySyncerInMem<PoWord>();
		var diff = syncer.DiffPoByTime(Local, Remote);
		return new DtoEntityDiffEtSync<PoWord>{
			LocalCompareToRemote = diff,
			SyncedEntity = diff < 0 ? Remote : null,
		};
	}

	/// 計算 Remote 合入 Local 的同步結果，包含差異分類與待寫回內容。
	public DtoJnWordSyncResult SyncJnWord(JnWord? Local, JnWord Remote){
		var r = new DtoJnWordSyncResult{
			Local = Local,
			Remote = Remote,
		};
		if(Local is null){
			r.DiffResult = EDiffByBizIdResultForSync.LocalNotExist;
			r.SyncedPoWord = Remote;
			return r;
		}

		var diff = this.CompareJnWord(Local, Remote);
		r.DiffResult = diff;
		if(diff == EDiffByBizIdResultForSync.NoChange || diff == EDiffByBizIdResultForSync.RemoteIsOlder){
			return r;
		}

		var (newProps, newLearns, changedProps, changedLearns) = DiffAssets(Local, Remote);
		var rootSync = SyncPoWord(Local.Word, Remote.Word);
		var diffCase = new AggDiffCaseForSync{
			RemoteHasNewAssets = newProps.Count > 0 || newLearns.Count > 0,
			RemoteHasChangedAssets = changedProps.Count > 0 || changedLearns.Count > 0,
			RemoteIsSoftDeleted = !Remote.DelAt.IsNullOrDefault(),
			LocalIsSoftDeleted = !Local.DelAt.IsNullOrDefault(),
			AggRootIsChanged = rootSync.SyncedEntity is not null,
		};
		r.DiffCase = diffCase;

		r.NewAssets = new JnWord{
			Word = Local.Word,
			Props = newProps,
			Learns = newLearns,
		};
		r.ChangedAssets = new JnWord{
			Word = Local.Word,
			Props = changedProps,
			Learns = changedLearns,
		};
		if(rootSync.SyncedEntity is not null){
			r.SyncedPoWord = new JnWord{
				Word = rootSync.SyncedEntity,
				Props = [],
				Learns = [],
			};
		}
		return r;
	}

	/// 對比資產列表，拆分爲「Remote 新增」與「Remote 變更」兩部分。
	(
		List<PoWordProp> NewProps,
		List<PoWordLearn> NewLearns,
		List<PoWordProp> ChangedProps,
		List<PoWordLearn> ChangedLearns
	) DiffAssets(JnWord Local, JnWord Remote){
		var newProps = new List<PoWordProp>();
		var newLearns = new List<PoWordLearn>();
		var changedProps = new List<PoWordProp>();
		var changedLearns = new List<PoWordLearn>();

		var localPropById = Local.Props.ToDictionary(x=>x.Id, x=>x);
		foreach(var remoteProp in Remote.Props){
			if(localPropById.TryGetValue(remoteProp.Id, out var localProp)){
				var diff = SyncProp(localProp, remoteProp);
				if(diff.SyncedEntity is not null){
					changedProps.Add(diff.SyncedEntity);
				}
				continue;
			}
			newProps.Add(remoteProp);
		}

		var localLearnById = Local.Learns.ToDictionary(x=>x.Id, x=>x);
		foreach(var remoteLearn in Remote.Learns){
			if(localLearnById.TryGetValue(remoteLearn.Id, out var localLearn)){
				var diff = SyncLearn(localLearn, remoteLearn);
				if(diff.SyncedEntity is not null){
					changedLearns.Add(diff.SyncedEntity);
				}
				continue;
			}
			newLearns.Add(remoteLearn);
		}

		return (newProps, newLearns, changedProps, changedLearns);
	}
}
