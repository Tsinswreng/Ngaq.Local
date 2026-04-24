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
using Tsinswreng.CsTextWithBlob;

public partial class SvcWordV2
{
	public async IAsyncEnumerable<DtoJnWordSyncResult> BatSyncJnWordByBizId(
		IDbUserCtx Ctx,
		IAsyncEnumerable<JnWord> JnWords,
		[EnumeratorCancellation] CT Ct
	){
		Ctx.DbFnCtx ??= new DbFnCtx();
		await using var batch = new BatchCollector<JnWord, IAsyncEnumerable<DtoJnWordSyncResult>>(async(remotes, Ct)=>{
			if(remotes.Count == 0){
				return ToAsyE(Array.Empty<DtoJnWordSyncResult>());
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

			var dtos = new List<DtoJnWordSyncResult>(remotes.Count);
			for(var i = 0; i < remotes.Count; i++){
				var localPo = localPos[i];
				JnWord? local = null;
				if(localPo is not null){
					local = localById.GetValueOrDefault(localPo.Id);
				}
				dtos.Add(SvcWordInMem.SyncJnWord(local, remotes[i]));
			}
			await BatSyncByDto(Ctx, ToAsyE(dtos), Ct);
			return ToAsyE(dtos);
		});

		var all = batch.AllFlat(JnWords, Ct);
		await foreach(var dto in all.WithCancellation(Ct)){
			yield return dto;
		}
	}

	public Task<nil> BatSyncByDto(IDbUserCtx Ctx, IAsyncEnumerable<DtoJnWordSyncResult> Dtos, CT Ct){
		return SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
			var localCtx = new DbUserCtx(Ctx.UserCtx, DbCtx);
			var noChange = new List<DtoJnWordSyncResult>();
			var remoteOlder = new List<DtoJnWordSyncResult>();
			var localNotExist = new List<DtoJnWordSyncResult>();
			var idNotEqual = new List<DtoJnWordSyncResult>();
			var remoteIsNewer = new List<DtoJnWordSyncResult>();

			await foreach(var dto in Dtos.WithCancellation(Ct)){
				if(dto.DiffResult == EDiffByBizIdResultForSync.NoChange){
					noChange.Add(dto);
				}else if(dto.DiffResult == EDiffByBizIdResultForSync.RemoteIsOlder){
					remoteOlder.Add(dto);
				}else if(dto.DiffResult == EDiffByBizIdResultForSync.LocalNotExist){
					localNotExist.Add(dto);
				}else if(dto.DiffResult == EDiffByBizIdResultForSync.IdNotEqual){
					idNotEqual.Add(dto);
				}else if(dto.DiffResult == EDiffByBizIdResultForSync.RemoteIsNewer){
					remoteIsNewer.Add(dto);
				}
			}

			await BatSync_NoChange(localCtx, ToAsyE(noChange), Ct);
			await BatSync_RemoteIsOlder(localCtx, ToAsyE(remoteOlder), Ct);
			await BatSync_LocalNotExist(localCtx, ToAsyE(localNotExist), Ct);
			await BatSync_IdNotEqual(localCtx, ToAsyE(idNotEqual), Ct);
			await BatSync_RemoteIsNewer(localCtx, ToAsyE(remoteIsNewer), Ct);
			return NIL;
		});
	}

	public async Task<nil> BatSync_NoChange(IDbUserCtx Ctx, IAsyncEnumerable<DtoJnWordSyncResult> Dtos, CT Ct){
		await foreach(var _ in Dtos.WithCancellation(Ct)){
		}
		return NIL;
	}

	public async Task<nil> BatSync_RemoteIsOlder(IDbUserCtx Ctx, IAsyncEnumerable<DtoJnWordSyncResult> Dtos, CT Ct){
		await foreach(var _ in Dtos.WithCancellation(Ct)){
		}
		return NIL;
	}

	public Task<nil> BatSync_LocalNotExist(IDbUserCtx Ctx, IAsyncEnumerable<DtoJnWordSyncResult> Dtos, CT Ct){
		return SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
			var neos = Dtos.Select(x=>{
				var remote = x.Remote ?? throw KeysErr.Word.Word__And__SyncFailed.ToErr("RemoteNull");
				remote.Owner = Ctx.UserCtx.UserId;
				remote.EnsureForeignId();
				return remote;
			});
			await RepoWord.BatAddAgg<JnWord>(DbCtx, neos, Ct);
			return NIL;
		});
	}

	public Task<nil> BatSync_IdNotEqual(IDbUserCtx Ctx, IAsyncEnumerable<DtoJnWordSyncResult> Dtos, CT Ct){
		return SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
			await using var batch = new BatchCollector<DtoJnWordSyncResult, nil>(async(dtoBatch, Ct)=>{
				var moveIds = new List<(IdWord Old, IdWord New)>();
				var remotesToApply = new List<JnWord>(dtoBatch.Count);
				foreach(var dto in dtoBatch){
					var local = dto.Local ?? throw KeysErr.Word.Word__And__SyncFailed.ToErr("LocalNull");
					var remote = dto.Remote ?? throw KeysErr.Word.Word__And__SyncFailed.ToErr("RemoteNull");
					local.Word.CheckOwner(Ctx.UserCtx.UserId);
					remote.Owner = Ctx.UserCtx.UserId;
					remote.EnsureForeignId();

					var keepId = local.Id.Value <= remote.Id.Value ? local.Id : remote.Id;
					if(local.Id != keepId){
						moveIds.Add((local.Id, keepId));
					}
					remote.SetIdEtEnsureFKey(keepId);
					remotesToApply.Add(remote);
				}
				if(moveIds.Count > 0){
					await BatChangeId(new DbUserCtx(Ctx.UserCtx, DbCtx), ToAsyE(moveIds), Ct);
				}
				await ApplyRemoteWordsAsBatchUpdate(DbCtx, remotesToApply, Ct);
				return NIL;
			});
			await batch.ConsumeAll(Dtos, Ct);
			return NIL;
		});
	}

	Task<nil> BatSync_RemoteIsNewer(IDbUserCtx Ctx, IAsyncEnumerable<DtoJnWordSyncResult> Dtos, CT Ct){
		return SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
			await using var batch = new BatchCollector<DtoJnWordSyncResult, nil>(async(dtoBatch, Ct)=>{
				var remotes = new List<JnWord>(dtoBatch.Count);
				foreach(var dto in dtoBatch){
					var remote = dto.Remote ?? throw KeysErr.Word.Word__And__SyncFailed.ToErr("RemoteNull");
					remote.Owner = Ctx.UserCtx.UserId;
					remote.EnsureForeignId();
					remotes.Add(remote);
				}
				await ApplyRemoteWordsAsBatchUpdate(DbCtx, remotes, Ct);
				return NIL;
			});
			await batch.ConsumeAll(Dtos, Ct);
			return NIL;
		});
	}

	async Task<nil> ApplyRemoteWordAsUpdate(IDbFnCtx DbCtx, JnWord Remote, CT Ct){
		await ApplyRemoteWordsAsBatchUpdate(DbCtx, [Remote], Ct);
		return NIL;
	}

	async Task<nil> ApplyRemoteWordsAsBatchUpdate(IDbFnCtx DbCtx, IList<JnWord> Remotes, CT Ct){
		if(Remotes.Count == 0){
			return NIL;
		}

		await RepoWord.BatUpd(DbCtx, ToAsyE(Remotes.Select(x=>x.Word)), Ct);
		var props = Remotes.SelectMany(x=>x.Props).ToList();
		if(props.Count > 0){
			await RepoProp.BatUpsert(DbCtx, ToAsyE(props), Ct);
		}
		var learns = Remotes.SelectMany(x=>x.Learns).ToList();
		if(learns.Count > 0){
			await RepoLearn.BatUpsert(DbCtx, ToAsyE(learns), Ct);
		}
		await DaoWordV2.BatAltWordAfterUpd(DbCtx, ToAsyE(Remotes.Select(x=>x.Id).Distinct()), Ct);
		return NIL;
	}
	public IAsyncEnumerable<DtoJnWordSyncResult> BatSyncJnWordByBizIdFromStream(
		IDbUserCtx Ctx,
		Stream TextWithStream,
		CT Ct
	){
		// 先把流中的壓縮詞流解包成 JnWord 流，再走既有 BizId 同步主流程。
		var words = UnpackJnWords(TextWithStream, Ct);
		return BatSyncJnWordByBizId(Ctx, words, Ct);
	}

	public IAsyncEnumerable<JnWord> GetAllWordsWithDel(IDbUserCtx Ctx, CT Ct){
		Ctx.DbFnCtx ??= new DbFnCtx();
		// 先取當前用戶全部根詞（含軟刪），再按 Id 批量取聚合（含軟刪）。
		var ids = RepoWord.GetAllWithDel(Ctx.DbFnCtx, Ct)
			.Where(x=>x.Owner == Ctx.UserCtx.UserId)
			.Select(x=>x.Id);
		return RepoWord.BatGetAggByIdWithDel<JnWord>(Ctx.DbFnCtx, ids, Ct)
			.Where(x=>x is not null)
			.Select(x=>x!);
	}

	public Task<Stream> PackAllWordsWithDel(IDbUserCtx Ctx, CT Ct){
		var packer = new Packer<JnWord>{
			JsonS = JsonS,
		};
		var packInfo = new ObjPackInfo{
			PayloadTypeObj = nameof(GZipLinesUtf8),
			CreatedAt = UnixMs.Now(),
		};
		var allWords = GetAllWordsWithDel(Ctx, Ct);
		var packed = packer.Pack(allWords, packInfo, Ct);
		return Task.FromResult(packed.ToStream());
	}

	public IAsyncEnumerable<JnWord> UnpackJnWords(Stream TextWithStream, CT Ct){
		return UnpackCore(TextWithStream, Ct);

		async IAsyncEnumerable<JnWord> UnpackCore(
			Stream textWithStream,
			[EnumeratorCancellation] CT ct
		){
			var unpacked = await Tsinswreng.CsTextWithBlob.TextWithStream.Unpack(textWithStream, ct);
			var packer = new Packer<JnWord>{
				JsonS = JsonS,
			};
			var ans = packer.Unpack(unpacked, ct);
			if(!ans.Ok || ans.Data is null){
				throw KeysErr.Word.Word__And__SyncFailed.ToErr(str.Join('\n', ans.ErrsToStrs()));
			}

			await foreach(var word in ans.Data.WithCancellation(ct)){
				yield return word;
			}
		}
	}
}
