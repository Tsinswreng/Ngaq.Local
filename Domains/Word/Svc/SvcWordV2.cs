namespace Ngaq.Local.Domains.Word.Svc;

using Ngaq.Core.Infra;
using Ngaq.Core.Infra.Errors;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Shared.StudyPlan.Models.PreFilter;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.Word.Models;
using Ngaq.Core.Shared.Word.Models.Learn_;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Shared.Word.Models.Po.Learn;
using Ngaq.Core.Shared.Word.Models.Po.Word;
using Ngaq.Core.Shared.Word.Svc;
using Ngaq.Local.Db.TswG;
using Ngaq.Local.Word.Dao;
using Tsinswreng.CsErr;
using Tsinswreng.CsSql;
using Tsinswreng.CsTools;

public partial class SvcWordV2(
	ISqlCmdMkr SqlCmdMkr
	,DaoWordV2 DaoWordV2
	,IRepo<PoWord, IdWord> RepoWord
	,IRepo<PoWordProp, IdWordProp> RepoProp
	,IRepo<PoWordLearn, IdWordLearn> RepoLearn
):ISvcWordV2
{
	ISqlCmdMkr SqlCmdMkr = SqlCmdMkr;
	DaoWordV2 DaoWordV2 = DaoWordV2;
	IRepo<PoWord, IdWord> RepoWord = RepoWord;
	IRepo<PoWordProp, IdWordProp> RepoProp = RepoProp;
	IRepo<PoWordLearn, IdWordLearn> RepoLearn = RepoLearn;

	public IAsyncEnumerable<JnWord> GetWordsToLearn(
		IDbUserCtx Ctx, CT Ct
	){
		return GetWordsToLearn(Ctx, null, Ct);
	}

	public IAsyncEnumerable<JnWord> GetWordsToLearn(
		IDbUserCtx Ctx, PreFilter? Prefilter, CT Ct
	){
		Ctx.DbFnCtx ??= new DbFnCtx();
		_ = Prefilter; // TODO: 待學習方案模塊完成後接入預篩選
		return DaoWordV2.GetWordsByOwner(Ctx.DbFnCtx, Ctx.UserCtx.UserId, Ct);
	}

	public async Task<nil> BatAddNewLearnRecord(
		IDbUserCtx Ctx
		,IAsyncEnumerable<PoWordLearn> PoWordLearnAsyE, CT Ct
	){
		return await SqlCmdMkr.RunInTxnIfNoCtx(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
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
		return SqlCmdMkr.RunInTxnIfNoCtx(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
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
			throw ItemsErr.Word.__And__IsNotSameUserWord.ToErr(ExiId, Owner);
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

	static JnWord MkMergedWord(IdUser Owner, JnWord Src){
		var R = new JnWord{
			Word = new PoWord{
				Id = Src.Word.Id,
				Owner = Owner,
				Head = Src.Head,
				Lang = Src.Lang,
				StoredAt = Src.StoredAt,
				BizCreatedAt = Src.BizCreatedAt,
				BizUpdatedAt = Src.BizUpdatedAt,
			},
		};
		R.Props = [];
		R.Learns = [];
		return R;
	}

	static PoWordProp ClonePropToWord(PoWordProp P, IdWord WordId){
		return new PoWordProp{
			Id = P.Id,
			WordId = WordId,
			KType = P.KType,
			KStr = P.KStr,
			KI64 = P.KI64,
			VType = P.VType,
			VStr = P.VStr,
			VI64 = P.VI64,
			VF64 = P.VF64,
			VBinary = P.VBinary,
			BizCreatedAt = P.BizCreatedAt,
			BizUpdatedAt = P.BizUpdatedAt,
		};
	}

	static PoWordLearn MkAddLearn(IdWord WordId, Tempus BizCreatedAt){
		return new PoWordLearn{
			WordId = WordId,
			LearnResult = ELearn.Add,
			BizCreatedAt = BizCreatedAt,
		};
	}
	public async Task<nil> SoftDelJnWordInId(
		IDbUserCtx Ctx,
		IAsyncEnumerable<IdWord> Ids, CT Ct
	){
		return await SqlCmdMkr.RunInTxnIfNoCtx(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
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
				throw ItemsErr.Word.__And__IsNotSameUserWord.ToErr(Id, UserId);
			}
			i++;
		}

		if(i != Ids.Count){
			throw ItemsErr.Word.__And__IsNotSameUserWord.ToErr(UserId);
		}
		return NIL;
	}

	static List<IdWord> DistinctWordIds(IList<IdWord> Ids){
		var Set = new HashSet<IdWord>();
		var R = new List<IdWord>();
		foreach(var Id in Ids){
			if(!Set.Add(Id)){
				continue;
			}
			R.Add(Id);
		}
		return R;
	}

	static IAsyncEnumerable<T> ToAsyE<T>(IEnumerable<T> Src){
		return ToolAsyE.ToAsyE(Src);
	}

	static bool IsDescription(PoWordProp Prop){
		return Prop.KStr == KeysProp.Inst.description;
	}

	static void AppendAddLearns(
		IList<PoWordLearn> Out
		,IdWord WordId
		,IEnumerable<PoWordProp> Props
	){
		foreach(var P in Props){
			if(!IsDescription(P)){
				continue;
			}
			Out.Add(new PoWordLearn{
				WordId = WordId,
				LearnResult = ELearn.Add,
				BizCreatedAt = P.BizCreatedAt,
			});
		}
	}

	static PropFingerprint MkPropFingerprint(PoWordProp P){
		var Binary = P.VBinary is null ? null : Convert.ToBase64String(P.VBinary);
		return new PropFingerprint(
			P.KType,
			P.KStr,
			P.KI64,
			P.VType,
			P.VStr,
			P.VI64,
			P.VF64,
			Binary
		);
	}

	sealed class MergedWord(Head_Lang Key, JnWord Word){
		public Head_Lang Key { get; } = Key;
		public JnWord Word { get; } = Word;
		public IdWord? ExistingId { get; set; }
	}

	sealed class WordBatchPlan{
		public List<JnWord> NeoAggs { get; } = [];
		public List<PoWordProp> NeoProps { get; } = [];
		public List<PoWordLearn> NeoLearns { get; } = [];
		public List<IdWord> TouchIds { get; } = [];
	}

	record struct PropFingerprint(
		EKvType KType,
		str? KStr,
		i64 KI64,
		EKvType VType,
		str? VStr,
		i64 VI64,
		f64 VF64,
		str? VBinaryBase64
	);
}
