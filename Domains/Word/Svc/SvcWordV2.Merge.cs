namespace Ngaq.Backend.Domains.Word.Svc;

using System.Runtime.CompilerServices;
using Ngaq.Core.Frontend.Kv;
using Ngaq.Core.Infra;
using Ngaq.Core.Infra.Errors;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Tools;
using Ngaq.Core.Shared.Sync;
using Ngaq.Core.Shared.StudyPlan.Models.PreFilter;
using Ngaq.Core.Shared.StudyPlan.Models.Po.PreFilter;
using Ngaq.Core.Shared.StudyPlan.Models.Po.StudyPlan;
using Ngaq.Core.Shared.StudyPlan.Svc;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.Word.Models;
using Ngaq.Core.Shared.Word.Models.Learn_;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Shared.Word.Models.Po.Learn;
using Ngaq.Core.Shared.Word.Models.Po.Word;
using Ngaq.Core.Shared.Word.Svc;
using Ngaq.Backend.Db.TswG;
using Tsinswreng.CsErr;
using Tsinswreng.CsCore;
using Tsinswreng.CsSql;
using Tsinswreng.CsTools;
using Ngaq.Core.Tools.Json;
using Ngaq.Core.Shared.Dictionary.Models;
using Ngaq.Core.Shared.Word.Models.Dto;
using Ngaq.Core.Shared.Base.Models.Po;
using Ngaq.Backend.Domains.Word;
using Ngaq.Backend.Domains.Word.Dao;
using Tsinswreng.CsTempus;

public partial class SvcWordV2
{
	/// 先按 BizId(Owner+Head+Lang) 查庫，再計算 Remote 合入 Local 的結果。
	public async IAsyncEnumerable<IJnWordMergeResult> GetWordMergeResult(
		IDbUserCtx Ctx,
		IAsyncEnumerable<JnWord> Words,
		[EnumeratorCancellation] CT Ct
	){
		Ctx.DbFnCtx ??= new DbFnCtx();
		await using var batch = new BatchCollector<JnWord, IAsyncEnumerable<IJnWordMergeResult>>(async(remotes, Ct)=>{
			if(remotes.Count == 0){
				return ToAsyE(Array.Empty<IJnWordMergeResult>());
			}

			foreach(var remote in remotes){
				remote.Owner = Ctx.UserCtx.UserId;
				remote.EnsureForeignId();
			}

			var headLangs = remotes.Select(x=>new Head_Lang(x.Head, x.Lang)).ToList();
			var localPos = await DaoWordV2.BatGetPoWordByOwnerHeadLangWithDel(
				Ctx.DbFnCtx,
				Ctx.UserCtx.UserId,
				ToAsyE(headLangs),
				Ct
			).ToListAsync(Ct);
			var ids = localPos.Where(x=>x is not null).Select(x=>x!.Id).Distinct().ToList();
			var localById = new Dictionary<IdWord, JnWord?>();
			if(ids.Count > 0){
				var locals = await DaoWordV2.BatGetJnWordByIdWithDel(Ctx.DbFnCtx, ToAsyE(ids), Ct).ToListAsync(Ct);
				for(var i = 0; i < ids.Count; i++){
					localById[ids[i]] = locals[i];
				}
			}

			var ans = new List<IJnWordMergeResult>(remotes.Count);
			for(var i = 0; i < remotes.Count; i++){
				JnWord? local = null;
				var localPo = localPos[i];
				if(localPo is not null){
					local = localById.GetValueOrDefault(localPo.Id);
				}
				ans.Add(SvcWordInMem.Merge(local, remotes[i]));
			}
			return ToAsyE(ans);
		});
		var all = batch.AllFlat(Words, Ct);
		await foreach(var one in all.WithCancellation(Ct)){
			yield return one;
		}
	}

	/// 實際把合併結果落庫：新增整詞、或把新增資產追加到已有單詞。
	public Task<nil> MergeWord(IDbUserCtx Ctx, IAsyncEnumerable<IJnWordMergeResult> Words, CT Ct){
		return SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
			await using var batch = new BatchCollector<IJnWordMergeResult, nil>(async(results, Ct)=>{
				if(results.Count == 0){
					return NIL;
				}

				var neoAggs = new List<JnWord>();
				var updRoots = new List<PoWord>();
				var neoProps = new List<PoWordProp>();
				var neoLearns = new List<PoWordLearn>();
				var touchIds = new List<IdWord>();
				foreach(var item in results){
					if(item.Result == EJnWordMergeResult.NoChange){
						continue;
					}
					var merged = item.Merged;
					merged.Owner = Ctx.UserCtx.UserId;
					merged.EnsureForeignId();
					if(item.Result == EJnWordMergeResult.LocalNotExist){
						neoAggs.Add(merged);
						continue;
					}

					updRoots.Add(merged.Word);
					var newAssets = item.NewAssets;
					if(newAssets is null){
						continue;
					}
					foreach(var p in newAssets.Props){
						p.WordId = merged.Id;
						neoProps.Add(p);
					}
					foreach(var l in newAssets.Learns){
						l.WordId = merged.Id;
						neoLearns.Add(l);
					}
					touchIds.Add(merged.Id);
				}

				if(neoAggs.Count > 0){
					await RepoWord.BatAddAgg<JnWord>(DbCtx, ToAsyE(neoAggs), Ct);
				}
				if(updRoots.Count > 0){
					await RepoWord.BatUpd(DbCtx, ToAsyE(updRoots), Ct);
				}
				if(neoProps.Count > 0){
					await RepoProp.BatAdd(DbCtx, ToAsyE(neoProps), Ct);
				}
				if(neoLearns.Count > 0){
					await RepoLearn.BatAdd(DbCtx, ToAsyE(neoLearns), Ct);
				}
				if(touchIds.Count > 0){
					await DaoWordV2.BatAltWordAfterUpd(DbCtx, ToAsyE(touchIds.Distinct()), Ct);
				}
				return NIL;
			});
			await batch.ConsumeAll(Words, Ct);
			return NIL;
		});
	}

	/// 先算合併結果，再把結果寫入數據庫。
	public Task<nil> MergeWord(IDbUserCtx Ctx, IAsyncEnumerable<JnWord> Words, CT Ct){
		var mergeResult = GetWordMergeResult(Ctx, Words, Ct);
		return MergeWord(Ctx, mergeResult, Ct);
	}

	/// 在 Merge 的基礎上，將「Remote 新增的 description」轉成 ELearn.Add 記錄。
	public Task<nil> MergeWord_NewDescrAsAdd(IDbUserCtx Ctx, IAsyncEnumerable<JnWord> Words, CT Ct){
		return SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
			var localCtx = new DbUserCtx(Ctx.UserCtx, DbCtx);
			var mergeResults = new List<IJnWordMergeResult>();
			var addLearns = new List<PoWordLearn>();

			await foreach(var one in GetWordMergeResult(localCtx, Words, Ct).WithCancellation(Ct)){
				mergeResults.Add(one);
				var wordId = one.Merged.Id;
				foreach(var p in one.NewAssets?.Props ?? []){
					if(!IsDescription(p)){
						continue;
					}
					addLearns.Add(MkAddLearn(wordId, p.BizCreatedAt));
				}
			}

			await MergeWord(localCtx, ToAsyE(mergeResults), Ct);
			if(addLearns.Count > 0){
				await RepoLearn.BatAdd(DbCtx, ToAsyE(addLearns), Ct);
				await DaoWordV2.BatAltWordAfterUpd(
					DbCtx,
					ToAsyE(addLearns.Select(x=>x.WordId).Distinct()),
					Ct
				);
			}
			return NIL;
		});
	}
}
