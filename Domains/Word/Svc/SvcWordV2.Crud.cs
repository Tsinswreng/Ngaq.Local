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
	public async Task<nil> BatAddNewLearnRecord(
		IDbUserCtx Ctx
		,IAsyncEnumerable<PoWordLearn> PoWordLearnAsyE, CT Ct
	){
		return await SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
			await using var Batch = new BatchCollector<PoWordLearn, nil>(async(Learns, Ct)=>{
				var DistinctIds = DistinctWordIds(Learns.Select(x=>x.WordId).ToList());
				if(DistinctIds.Count == 0){
					return NIL;
				}
				await EnsureOwner(DbCtx, Ctx.UserCtx.UserId, DistinctIds, Ct);
				await RepoLearn.BatAdd(DbCtx, ToAsyE(Learns), Ct);
				await DaoWordV2.BatAltWordAfterUpd(DbCtx, ToAsyE(DistinctIds), Ct);
				return NIL;
			});

			await foreach(var One in PoWordLearnAsyE){
				await Batch.Add(One, Ct);
			}
			await Batch.End(Ct);
			return NIL;
		});
	}
	
	public Task<nil> BatAddNewWordToLearn(
		IDbUserCtx Ctx,
		IAsyncEnumerable<JnWord> Words, CT Ct
	){
		return SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
			return await BatAddNewWordToLearnCore(
				DbCtx,
				Ctx.UserCtx.UserId,
				Words,
				Ct
			);
		});
	}

	async Task<nil> BatAddNewWordToLearnCore(
		IDbFnCtx DbCtx,
		IdUser Owner,
		IAsyncEnumerable<JnWord> Words,
		CT Ct
	){
		await using var Batch = new BatchCollector<JnWord, nil>(
			async(WordBatch, Ct)=> await ProcessWordBatch(DbCtx, Owner, WordBatch, Ct)
		);
		
		await Batch.ConsumeAll(Words, Ct);
		return NIL;
	}

	async Task<nil> ProcessWordBatch(
		IDbFnCtx DbCtx,
		IdUser Owner,
		IList<JnWord> WordBatch,
		CT Ct
	){
		if(WordBatch.Count == 0){
			return NIL;
		}
		var Merged = MergeWordBatch(Owner, WordBatch);
		await FillExistingIds(DbCtx, Owner, Merged, Ct);
		var ExistingById = await LoadExistingWordsById(DbCtx, Merged, Ct);
		var Plan = BuildBatchPlan(Owner, Merged, ExistingById);
		await ApplyBatchPlan(DbCtx, Plan, Ct);
		return NIL;
	}

	List<MergedWord> MergeWordBatch(IdUser Owner, IList<JnWord> WordBatch){
		var Merged = new List<MergedWord>(WordBatch.Count);
		var KeyToIndex = new Dictionary<Head_Lang, int>();
		foreach(var Src in WordBatch){
			var Key = new Head_Lang(Src.Head, Src.Lang);
			if(!KeyToIndex.TryGetValue(Key, out var i)){
				var Neo = MkMergedWord(Owner, Src);
				Merged.Add(new MergedWord(Key, Neo));
				i = Merged.Count - 1;
				KeyToIndex[Key] = i;
			}
			foreach(var P in Src.Props){
				Merged[i].Word.Props.Add(P);
			}
		}
		return Merged;
	}

	async Task FillExistingIds(
		IDbFnCtx DbCtx,
		IdUser Owner,
		IList<MergedWord> Merged,
		CT Ct
	){
		var ExistingIds = DaoWordV2.BatGetIdByOwnerHeadLang(
			DbCtx,
			Owner,
			ToAsyE(Merged.Select(x=>x.Key)),
			Ct
		);
		var Pos = 0;
		await foreach(var Id in ExistingIds){
			if(Pos >= Merged.Count){
				break;
			}
			Merged[Pos].ExistingId = Id;
			Pos++;
		}
	}

	async Task<Dictionary<IdWord, JnWord>> LoadExistingWordsById(
		IDbFnCtx DbCtx,
		IEnumerable<MergedWord> Merged,
		CT Ct
	){
		var Ids = Merged.Where(x=>x.ExistingId is not null)
			.Select(x=>x.ExistingId!.Value)
			.ToList();
		var R = new Dictionary<IdWord, JnWord>();
		if(Ids.Count == 0){
			return R;
		}
		var ExistingWords = DaoWordV2.BatGetJnWordByIdWithDel(DbCtx, ToAsyE(Ids), Ct);
		var i = 0;
		await foreach(var Exi in ExistingWords){
			if(i >= Ids.Count){
				break;
			}
			if(Exi is not null){
				R[Ids[i]] = Exi;
			}
			i++;
		}
		return R;
	}

	WordBatchPlan BuildBatchPlan(
		IdUser Owner,
		IEnumerable<MergedWord> Merged,
		IDictionary<IdWord, JnWord> ExistingById
	){
		var Plan = new WordBatchPlan();
		foreach(var One in Merged){
			if(One.ExistingId is null){
				AddNewWordToPlan(Owner, One, Plan);
				continue;
			}
			AddExistingWordToPlan(Owner, One, ExistingById, Plan);
		}
		return Plan;
	}

	void AddNewWordToPlan(IdUser Owner, MergedWord One, WordBatchPlan Plan){
		One.Word.Owner = Owner;
		One.Word.EnsureForeignId();
		Plan.NeoAggs.Add(One.Word);
		AppendAddLearns(Plan.NeoLearns, One.Word.Id, One.Word.Props);
	}

	void AddExistingWordToPlan(
		IdUser Owner,
		MergedWord One,
		IDictionary<IdWord, JnWord> ExistingById,
		WordBatchPlan Plan
	){
		var ExiId = One.ExistingId!.Value;
		if(!ExistingById.TryGetValue(ExiId, out var ExiWord)){
			throw KeysErr.Word.__And__IsNotSameUserWord.ToErr(ExiId, Owner);
		}
		var Known = new HashSet<PropFingerprint>();
		foreach(var P in ExiWord.Props){
			Known.Add(MkPropFingerprint(P));
		}
		var AddedAny = false;
		foreach(var P in One.Word.Props){
			if(!Known.Add(MkPropFingerprint(P))){
				continue;
			}
			var NeoP = ClonePropToWord(P, ExiId);
			Plan.NeoProps.Add(NeoP);
			if(IsDescription(NeoP)){
				Plan.NeoLearns.Add(MkAddLearn(ExiId, NeoP.BizCreatedAt));
			}
			AddedAny = true;
		}
		if(AddedAny){
			Plan.TouchIds.Add(ExiId);
		}
	}

	async Task<nil> ApplyBatchPlan(IDbFnCtx DbCtx, WordBatchPlan Plan, CT Ct){
		if(Plan.NeoAggs.Count > 0){
			await RepoWord.BatAddAgg<JnWord>(DbCtx, ToAsyE(Plan.NeoAggs), Ct);
		}
		if(Plan.NeoProps.Count > 0){
			await RepoProp.BatAdd(DbCtx, ToAsyE(Plan.NeoProps), Ct);
		}
		if(Plan.NeoLearns.Count > 0){
			await RepoLearn.BatAdd(DbCtx, ToAsyE(Plan.NeoLearns), Ct);
		}
		var DistinctTouched = DistinctWordIds(Plan.TouchIds);
		if(DistinctTouched.Count > 0){
			await DaoWordV2.BatAltWordAfterUpd(DbCtx, ToAsyE(DistinctTouched), Ct);
		}
		return NIL;
	}

	public async Task<nil> SoftDelJnWordInId(
		IDbUserCtx Ctx,
		IAsyncEnumerable<IdWord> Ids, CT Ct
	){
		return await SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
			await using var Batch = new BatchCollector<IdWord, nil>(async(IdBatch, Ct)=>{
				var DistinctIds = DistinctWordIds(IdBatch);
				if(DistinctIds.Count == 0){
					return NIL;
				}
				await EnsureOwner(DbCtx, Ctx.UserCtx.UserId, DistinctIds, Ct);
				await RepoWord.SoftDelAggInId<JnWord>(DbCtx, ToAsyE(DistinctIds), Ct);
				return NIL;
			});

			await foreach(var One in Ids){
				await Batch.Add(One, Ct);
			}
			await Batch.End(Ct);
			return NIL;
		});
	}

	async Task<nil> EnsureOwner(
		IDbFnCtx Ctx
		,IdUser UserId
		,IReadOnlyList<IdWord> Ids
		,CT Ct
	){
		var Got = DaoWordV2.BatGetPoWordByIdWithDel(Ctx, ToAsyE(Ids), Ct);
		var i = 0;
		await foreach(var Po in Got){
			if(i >= Ids.Count){
				break;
			}
			var Id = Ids[i];
			if(Po is null || Po.Owner != UserId){
				throw KeysErr.Word.__And__IsNotSameUserWord.ToErr(Id, UserId);
			}
			i++;
		}

		if(i != Ids.Count){
			throw KeysErr.Word.__And__IsNotSameUserWord.ToErr(UserId);
		}
		return NIL;
	}

	public async Task<JnWord> LlmDictWordToJnWord(IDbUserCtx Ctx, IReqLlmDict Req, IRespLlmDict LlmDict, CT Ct){
		var SrcLang = Req.OptLang.SrcLang;
		var NormLangType = SrcLang.Type == ELangIdentType.Unknown ? ELangIdentType.Bcp47 : SrcLang.Type;
		var NormLangCode = string.IsNullOrWhiteSpace(SrcLang.Code) ? "en" : SrcLang.Code.Trim();
		var Lang = await ResolveUserLangByNormLang(Ctx, NormLangType, NormLangCode, Ct);
		var Now = Tempus.Now();

		var R = new JnWord{
			Word = new PoWord{
				Owner = Ctx.UserCtx.UserId,
				Head = (LlmDict.Head ?? "").Trim(),
				Lang = Lang,
				StoredAt = Now,
				BizCreatedAt = Now,
				BizUpdatedAt = Now,
			},
			Props = [],
			Learns = [],
		};

		AppendDescrProps(R.Props, LlmDict.Descrs, Now);
		AppendPronunciationProps(R.Props, LlmDict.Pronunciations, Now);
		R.EnsureForeignId();
		return R;
	}
	
	public async Task<JnWord> LlmDictWordToJnWordWithLearn(
		IDbUserCtx Ctx
		,IReqLlmDict Req
		,IRespLlmDict LlmDict, CT Ct
	){
		var Jnword = await LlmDictWordToJnWord(Ctx, Req, LlmDict, Ct);
		var learn = new PoWordLearn{
			WordId = Jnword.Word.Id,
			LearnResult = ELearn.Add,
			BizCreatedAt = Tempus.Now(),
		};
		Jnword.Learns.Add(learn);
		return Jnword;
	}

	/// 批量新增整詞聚合。先統一 Owner，再確保資產外鍵指向聚合根。
	public Task<nil> BatAddJnWord(IDbUserCtx Ctx, IAsyncEnumerable<JnWord> Words, CT Ct){
		return SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
			var owner = Ctx.UserCtx.UserId;
			var normalized = Words.Select(w=>{
				w.Owner = owner;
				w.EnsureForeignId();
				return w;
			});
			try{
				await RepoWord.BatAddAgg<JnWord>(DbCtx, normalized, Ct);
			}catch(Exception e){
				throw KeysErr.Common.DataIllegalOrConflict.ToErr(e.Message);
			}
			return NIL;
		});
	}

	/// 批量更新屬性資產。需先校驗全部屬性所屬單詞的 Owner。
	public Task<nil> BatUpdWordProp(IDbUserCtx Ctx, IAsyncEnumerable<PoWordProp> WordProps, CT Ct){
		return SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
			await using var batch = new BatchCollector<PoWordProp, nil>(async(propBatch, Ct)=>{
				if(propBatch.Count == 0){
					return NIL;
				}
				var wordIds = DistinctWordIds(propBatch.Select(x=>x.WordId).ToList());
				if(wordIds.Count == 0){
					return NIL;
				}
				await EnsureOwner(DbCtx, Ctx.UserCtx.UserId, wordIds, Ct);
				await RepoProp.BatUpd(DbCtx, ToAsyE(propBatch), Ct);
				await DaoWordV2.BatAltWordAfterUpd(DbCtx, ToAsyE(wordIds), Ct);
				return NIL;
			});
			await batch.ConsumeAll(WordProps, Ct);
			return NIL;
		});
	}

	/// 批量軟刪屬性資產，並觸碰其聚合根 BizUpdatedAt。
	public Task<nil> DelWordPropInId(IDbUserCtx Ctx, IAsyncEnumerable<IdWordProp> Ids, CT Ct){
		return SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
			await using var batch = new BatchCollector<IdWordProp, nil>(async(idBatch, Ct)=>{
				if(idBatch.Count == 0){
					return NIL;
				}
				var props = await RepoProp.BatGetByIdWithDel(DbCtx, ToAsyE(idBatch), Ct).ToListAsync(Ct);
				var wordIds = DistinctWordIds(props.Where(x=>x is not null).Select(x=>x!.WordId).ToList());
				if(wordIds.Count > 0){
					await EnsureOwner(DbCtx, Ctx.UserCtx.UserId, wordIds, Ct);
				}
				await RepoProp.SoftDelInId(DbCtx, ToAsyE(idBatch), Ct);
				if(wordIds.Count > 0){
					await DaoWordV2.BatAltWordAfterUpd(DbCtx, ToAsyE(wordIds), Ct);
				}
				return NIL;
			});
			await batch.ConsumeAll(Ids, Ct);
			return NIL;
		});
	}

	/// 批量軟刪學習資產，並觸碰其聚合根 BizUpdatedAt。
	public Task<nil> DelWordLearnInId(IDbUserCtx Ctx, IAsyncEnumerable<IdWordLearn> Ids, CT Ct){
		return SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
			await using var batch = new BatchCollector<IdWordLearn, nil>(async(idBatch, Ct)=>{
				if(idBatch.Count == 0){
					return NIL;
				}
				var learns = await RepoLearn.BatGetByIdWithDel(DbCtx, ToAsyE(idBatch), Ct).ToListAsync(Ct);
				var wordIds = DistinctWordIds(learns.Where(x=>x is not null).Select(x=>x!.WordId).ToList());
				if(wordIds.Count > 0){
					await EnsureOwner(DbCtx, Ctx.UserCtx.UserId, wordIds, Ct);
				}
				await RepoLearn.SoftDelInId(DbCtx, ToAsyE(idBatch), Ct);
				if(wordIds.Count > 0){
					await DaoWordV2.BatAltWordAfterUpd(DbCtx, ToAsyE(wordIds), Ct);
				}
				return NIL;
			});
			await batch.ConsumeAll(Ids, Ct);
			return NIL;
		});
	}

	/// 先按 Head/Lang 需要時遷移目標詞，再更新其餘字段。
	public Task<IAsyncEnumerable<RespUpdPoWord>> BatUpdPoWord(IDbUserCtx Ctx, IAsyncEnumerable<PoWord> PoWords, CT Ct){
		return SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
			var results = new List<RespUpdPoWord>();
			await foreach(var arg in PoWords.WithCancellation(Ct)){
				var old = await DaoWordV2.BatGetPoWordByIdWithDel(DbCtx, ToAsyE([arg.Id]), Ct).FirstOrDefaultAsync(Ct);
				if(old is null){
					throw KeysErr.Word.WordOfId__NotFound.ToErr(arg.Id);
				}
				old.CheckOwner(Ctx.UserCtx.UserId);

				var hasUpdatedBizId = old.Head != arg.Head || old.Lang != arg.Lang;
				IdWord finalId = arg.Id;
				RespUpdBizId? respUpdBizId = null;
				if(hasUpdatedBizId){
					var headLangR = await BatUpdHeadLangCore(DbCtx, Ctx.UserCtx.UserId, ToAsyE([arg]), Ct);
					if(headLangR.Count > 0){
						respUpdBizId = headLangR[0];
						finalId = respUpdBizId.FinalId;
					}
				}

				var upd = new Dictionary<str, obj?>{
					[nameof(PoWord.Owner)] = Ctx.UserCtx.UserId,
					[nameof(PoWord.StoredAt)] = arg.StoredAt,
					[nameof(PoWord.BizCreatedAt)] = arg.BizCreatedAt,
					[nameof(PoWord.BizUpdatedAt)] = arg.BizUpdatedAt,
				};
				await RepoWord.BatUpdByCodeDict(DbCtx, ToAsyE([finalId]), ToAsyE([upd]), Ct);
				await DaoWordV2.BatAltWordAfterUpd(DbCtx, ToAsyE([finalId]), Ct);
				results.Add(new RespUpdPoWord{
					HasUpdatedBizId = hasUpdatedBizId,
					RespUpdBizId = respUpdBizId,
					FinalId = finalId,
				});
			}
			return ToAsyE(results);
		});
	}

	/// 批量改主鍵，並同步遷移資產外鍵。
	public Task<nil> BatChangeId(IDbUserCtx Ctx, IAsyncEnumerable<(IdWord Old, IdWord New)> Ids, CT Ct){
		return SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
			await using var batch = new BatchCollector<(IdWord Old, IdWord New), nil>(async(idBatch, Ct)=>{
				var moves = idBatch.Where(x=>x.Old != x.New).ToList();
				if(moves.Count == 0){
					return NIL;
				}

				// step 1: 檢查 old 一定存在且屬於當前用戶；new 不能已存在於庫中。
				foreach(var (oldId, newId) in moves){
					var old = await DaoWordV2.BatGetPoWordByIdWithDel(DbCtx, ToAsyE([oldId]), Ct).FirstOrDefaultAsync(Ct);
					if(old is null){
						throw KeysErr.Word.WordOfId__NotFound.ToErr(oldId);
					}
					old.CheckOwner(Ctx.UserCtx.UserId);

					var neo = await DaoWordV2.BatGetPoWordByIdWithDel(DbCtx, ToAsyE([newId]), Ct).FirstOrDefaultAsync(Ct);
					if(neo is not null){
						throw KeysErr.Common.DataIllegalOrConflict.ToErr(oldId, newId);
					}
				}

				// step 2: 主鍵與資產外鍵一併遷移。
				await DaoWordV2.BatChangeWordId(DbCtx, ToAsyE(moves), Ct);
				return NIL;
			});
			await batch.ConsumeAll(Ids, Ct);
			return NIL;
		});
	}

	/// 批量更新 Head/Lang，遇到衝突時按接口注釋要求做資產合併。
	public async IAsyncEnumerable<RespUpdBizId> BatUpdHeadLang(
		IDbUserCtx Ctx,
		IAsyncEnumerable<PoWord> PoWords,
		[EnumeratorCancellation] CT Ct
	){
		var r = await SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
			return await BatUpdHeadLangCore(DbCtx, Ctx.UserCtx.UserId, PoWords, Ct);
		});
		foreach(var one in r){
			yield return one;
		}
	}

	async Task<List<RespUpdBizId>> BatUpdHeadLangCore(
		IDbFnCtx DbCtx,
		IdUser UserId,
		IAsyncEnumerable<PoWord> PoWords,
		CT Ct
	){
		await using var batch = new BatchCollector<PoWord, IList<RespUpdBizId>>(async(args, Ct)=>{
			if(args.Count == 0){
				return [];
			}

			// step 1: 批量查當前 Id 對應的舊詞，按位置對齊校驗存在與權限。
			var inputIds = args.Select(x=>x.Id).ToList();
			var wordsOfId = await DaoWordV2.BatGetPoWordByIdWithDel(DbCtx, ToAsyE(inputIds), Ct).ToListAsync(Ct);
			// step 2: 批量查目標 (Head,Lang) 對應詞（含軟刪），後續按索引一一處理。
			var keys = args.Select(x=>new Head_Lang(x.Head, x.Lang)).ToList();
			var targets = await DaoWordV2.BatGetPoWordByOwnerHeadLangWithDel(DbCtx, UserId, ToAsyE(keys), Ct).ToListAsync(Ct);

			var r = new List<RespUpdBizId>(args.Count);
			var updHeadLangArgs = new List<(IdWord Id, str Head, str Lang)>();
			var restoreIds = new List<IdWord>();
			var softDelIds = new List<IdWord>();
			var moveAssetsArgs = new List<(IdWord Old, IdWord New)>();
			var touchIds = new List<IdWord>();

			for(var i = 0; i < args.Count; i++){
				var arg = args[i];
				var wordOfId = wordsOfId[i];
				if(wordOfId is null){
					throw KeysErr.Word.WordOfId__NotFound.ToErr(arg.Id);
				}
				wordOfId.CheckOwner(UserId);

				if(wordOfId.Head == arg.Head && wordOfId.Lang == arg.Lang){
					r.Add(new RespUpdBizId{
						Result = EUpdBizIdResult.BizIdAlreadyEqual,
						FinalId = wordOfId.Id,
					});
					continue;
				}

				var target = targets[i];
				if(target is null){
					updHeadLangArgs.Add((wordOfId.Id, arg.Head, arg.Lang));
					r.Add(new RespUpdBizId{
						Result = EUpdBizIdResult.DataOfBizIdNotExist,
						FinalId = wordOfId.Id,
					});
					continue;
				}

				if(!target.DelAt.IsNullOrDefault()){
					restoreIds.Add(target.Id);
				}

				// step 3: 目標已存在時，按規則軟刪舊詞並遷移資產。
				softDelIds.Add(wordOfId.Id);
				moveAssetsArgs.Add((wordOfId.Id, target.Id));
				touchIds.Add(wordOfId.Id);
				touchIds.Add(target.Id);
				r.Add(new RespUpdBizId{
					Result = EUpdBizIdResult.BizIdNotEqual,
					FinalId = target.Id,
				});
			}

			if(updHeadLangArgs.Count > 0){
				await DaoWordV2.BatUpdHeadLangById(DbCtx, ToAsyE(updHeadLangArgs), Ct);
			}
			if(restoreIds.Count > 0){
				await DaoWordV2.BatRestoreInId(DbCtx, ToAsyE(restoreIds.Distinct()), Ct);
			}
			if(softDelIds.Count > 0){
				await RepoWord.BatSoftDelById(DbCtx, ToAsyE(softDelIds.Distinct()), Ct);
			}
			if(moveAssetsArgs.Count > 0){
				await DaoWordV2.BatMoveAssetsToWordId(DbCtx, ToAsyE(moveAssetsArgs), Ct);
			}
			if(touchIds.Count > 0){
				await DaoWordV2.BatAltWordAfterUpd(DbCtx, ToAsyE(touchIds.Distinct()), Ct);
			}
			return r;
		});

		var ans = await batch.ConsumeAll(PoWords, Ct);
		return ans.SelectMany(x=>x).ToList();
	}
	
	/// 批量新增屬性資產。需先校驗全部屬性所屬單詞的 Owner。
	public Task<nil> BatAddWordProp(IDbUserCtx Ctx, IAsyncEnumerable<PoWordProp> WordProps, CT Ct){
		return SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
			await using var batch = new BatchCollector<PoWordProp, nil>(async(propBatch, Ct)=>{
				if(propBatch.Count == 0){
					return NIL;
				}
				var wordIds = DistinctWordIds(propBatch.Select(x=>x.WordId).ToList());
				if(wordIds.Count == 0){
					return NIL;
				}
				await EnsureOwner(DbCtx, Ctx.UserCtx.UserId, wordIds, Ct);
				await RepoProp.BatAdd(DbCtx, ToAsyE(propBatch), Ct);
				await DaoWordV2.BatAltWordAfterUpd(DbCtx, ToAsyE(wordIds), Ct);
				return NIL;
			});
			await batch.ConsumeAll(WordProps, Ct);
			return NIL;
		});
	}

	/// 批量新增學習資產。需先校驗全部學習記錄所屬單詞的 Owner。
	public Task<nil> BatAddWordLearn(IDbUserCtx Ctx, IAsyncEnumerable<PoWordLearn> WordLearns, CT Ct){
		return SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
			await using var batch = new BatchCollector<PoWordLearn, nil>(async(learnBatch, Ct)=>{
				if(learnBatch.Count == 0){
					return NIL;
				}
				var wordIds = DistinctWordIds(learnBatch.Select(x=>x.WordId).ToList());
				if(wordIds.Count == 0){
					return NIL;
				}
				await EnsureOwner(DbCtx, Ctx.UserCtx.UserId, wordIds, Ct);
				await RepoLearn.BatAdd(DbCtx, ToAsyE(learnBatch), Ct);
				await DaoWordV2.BatAltWordAfterUpd(DbCtx, ToAsyE(wordIds), Ct);
				return NIL;
			});
			await batch.ConsumeAll(WordLearns, Ct);
			return NIL;
		});
	}

	/// 批量更新學習資產。需先校驗全部學習記錄所屬單詞的 Owner。
	public Task<nil> BatUpdWordLearn(IDbUserCtx Ctx, IAsyncEnumerable<PoWordLearn> WordLearns, CT Ct){
		return SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
			await using var batch = new BatchCollector<PoWordLearn, nil>(async(learnBatch, Ct)=>{
				if(learnBatch.Count == 0){
					return NIL;
				}
				var wordIds = DistinctWordIds(learnBatch.Select(x=>x.WordId).ToList());
				if(wordIds.Count == 0){
					return NIL;
				}
				await EnsureOwner(DbCtx, Ctx.UserCtx.UserId, wordIds, Ct);
				await RepoLearn.BatUpd(DbCtx, ToAsyE(learnBatch), Ct);
				await DaoWordV2.BatAltWordAfterUpd(DbCtx, ToAsyE(wordIds), Ct);
				return NIL;
			});
			await batch.ConsumeAll(WordLearns, Ct);
			return NIL;
		});
	}

	
	
}
