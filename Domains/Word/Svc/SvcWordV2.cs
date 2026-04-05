namespace Ngaq.Local.Domains.Word.Svc;

using System.Runtime.CompilerServices;
using Ngaq.Core.Frontend.Kv;
using Ngaq.Core.Infra;
using Ngaq.Core.Infra.Errors;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Tools;
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
using Ngaq.Local.Db.TswG;
using Ngaq.Local.Word.Dao;
using Tsinswreng.CsErr;
using Tsinswreng.CsCore;
using Tsinswreng.CsSql;
using Tsinswreng.CsTools;
using Ngaq.Core.Tools.Json;

public partial class SvcWordV2(
	ISqlCmdMkr SqlCmdMkr
	,DaoWordV2 DaoWordV2
	,IRepo<PoWord, IdWord> RepoWord
	,IRepo<PoWordProp, IdWordProp> RepoProp
	,IRepo<PoWordLearn, IdWordLearn> RepoLearn
	,IRepo<PoStudyPlan, IdStudyPlan> RepoStudyPlan
	,IRepo<PoPreFilter, IdPreFilter> RepoPreFilter
	,ISvcStudyPlan SvcStudyPlan
	,IJsonSerializer JsonS
):ISvcWordV2
{
	ISqlCmdMkr SqlCmdMkr = SqlCmdMkr;
	DaoWordV2 DaoWordV2 = DaoWordV2;
	IRepo<PoWord, IdWord> RepoWord = RepoWord;
	IRepo<PoWordProp, IdWordProp> RepoProp = RepoProp;
	IRepo<PoWordLearn, IdWordLearn> RepoLearn = RepoLearn;
	IRepo<PoStudyPlan, IdStudyPlan> RepoStudyPlan = RepoStudyPlan;
	IRepo<PoPreFilter, IdPreFilter> RepoPreFilter = RepoPreFilter;
	ISvcStudyPlan SvcStudyPlan = SvcStudyPlan;

	/// <summary>
	/// 取得待学习单词（按当前学习方案的前置筛选器过滤；若未设置则返回用户全部词）。
	/// </summary>
	/// <param name="Ctx">数据库与用户上下文。</param>
	/// <param name="Ct">取消令牌。</param>
	/// <returns>流式返回的单词序列。</returns>
	public async IAsyncEnumerable<JnWord> GetWordsToLearn(
		IDbUserCtx Ctx, [EnumeratorCancellation] CT Ct
	){
		Ctx.DbFnCtx ??= new DbFnCtx();
		var preFilter = await GetCurStudyPlanPreFilter(Ctx, Ct);
		await foreach(var word in GetWordsToLearn(Ctx, preFilter, Ct).WithCancellation(Ct)){
			yield return word;
		}
	}

	/// <summary>
	/// 取得待学习单词（按入参前置筛选器过滤；若入参为null则返回用户全部词）。
	/// </summary>
	/// <param name="Ctx">数据库与用户上下文。</param>
	/// <param name="Prefilter">前置筛选器；传null表示不过滤。</param>
	/// <param name="Ct">取消令牌。</param>
	/// <returns>流式返回的单词序列。</returns>
	public IAsyncEnumerable<JnWord> GetWordsToLearn(
		IDbUserCtx Ctx, PreFilter? Prefilter, [EnumeratorCancellation] CT Ct
	){
		Ctx.DbFnCtx ??= new DbFnCtx();
		var words = DaoWordV2.GetWordsByOwnerByPreFilterSql(Ctx.DbFnCtx, Ctx.UserCtx.UserId, Prefilter, Ct);
		return words;
		//await foreach(var word in words.WithCancellation(Ct)){
			
		// 	if(!IsMatchedByPreFilter(word, Prefilter)){
		// 		continue;
		// 	}
		// 	yield return word;
		// }
	}

	async Task<PreFilter?> GetCurStudyPlanPreFilter(IDbUserCtx Ctx, CT Ct){
		var studyPlanId = await SvcStudyPlan.GetCurStudyPlanId(Ctx, Ct);
		if(studyPlanId is not IdStudyPlan spId || spId.IsNullOrDefault()){
			return null;
		}

		var poStudyPlan = await RepoStudyPlan.BatGetByIdWithDel(Ctx.DbFnCtx!, ToAsyE([spId]), Ct)
			.FirstOrDefaultAsync(Ct);
		if(poStudyPlan is null || poStudyPlan.Owner != Ctx.UserCtx.UserId){
			return null;
		}
		if(poStudyPlan.PreFilterId.IsNullOrDefault()){
			return null;
		}

		var poPreFilter = await RepoPreFilter.BatGetByIdWithDel(Ctx.DbFnCtx!, ToAsyE([poStudyPlan.PreFilterId]), Ct)
			.FirstOrDefaultAsync(Ct);
		if(poPreFilter is null || poPreFilter.Owner != Ctx.UserCtx.UserId){
			return null;
		}
		if(poPreFilter.Type != EPreFilterType.Json || string.IsNullOrWhiteSpace(poPreFilter.Text)){
			return null;
		}

		var json = poPreFilter.Text;
		if(string.IsNullOrWhiteSpace(json)){
			return null;
		}
		return JsonS.Parse<PreFilter>(json);
	}

	static bool IsMatchedByPreFilter(JnWord Word, PreFilter? PreFilter){
		if(PreFilter is null){
			return true;
		}
		if(!IsMatchedByFieldFilters(Word, PreFilter.CoreFilter, TryGetCoreFieldValues)){
			return false;
		}
		if(!IsMatchedByFieldFilters(Word, PreFilter.PropFilter, TryGetPropFieldValues)){
			return false;
		}
		return true;
	}

	delegate bool TryGetFieldValues(JnWord Word, str Field, out IReadOnlyList<obj?> Values);

	static bool IsMatchedByFieldFilters(
		JnWord Word,
		IList<FieldsFilter>? FieldFilters,
		TryGetFieldValues TryGetValues
	){
		if(FieldFilters is null || FieldFilters.Count == 0){
			return true;
		}
		foreach(var fieldFilter in FieldFilters){
			if(IsMatchedByOneFieldsFilter(Word, fieldFilter, TryGetValues)){
				continue;
			}
			return false;
		}
		return true;
	}

	static bool IsMatchedByOneFieldsFilter(
		JnWord Word,
		FieldsFilter FieldFilter,
		TryGetFieldValues TryGetValues
	){
		if(FieldFilter.Fields.Count == 0){
			return true;
		}
		foreach(var field in FieldFilter.Fields){
			if(!TryGetValues(Word, field, out var values)){
				continue;
			}
			if(IsMatchedByFilterItems(values, FieldFilter.Filters)){
				return true;
			}
		}
		return false;
	}

	static bool IsMatchedByFilterItems(
		IReadOnlyList<obj?> CandidateValues,
		IList<FilterItem> FilterItems
	){
		foreach(var filterItem in FilterItems){
			if(IsMatchedByFilterItem(CandidateValues, filterItem)){
				continue;
			}
			return false;
		}
		return true;
	}

	static bool IsMatchedByFilterItem(
		IReadOnlyList<obj?> CandidateValues,
		FilterItem FilterItem
	){
		var filterValues = FilterItem.Values ?? [];
		return FilterItem.Operation switch{
			EFilterOperationMode.IncludeAny => filterValues.Any(v=>CandidateValues.Any(c=>AreEqual(c, v, FilterItem.ValueType))),
			EFilterOperationMode.IncludeAll => filterValues.All(v=>CandidateValues.Any(c=>AreEqual(c, v, FilterItem.ValueType))),
			EFilterOperationMode.ExcludeAll => filterValues.All(v=>CandidateValues.All(c=>!AreEqual(c, v, FilterItem.ValueType))),
			EFilterOperationMode.Eq => CandidateValues.Any(c=>AreEqual(c, filterValues.FirstOrDefault(), FilterItem.ValueType)),
			EFilterOperationMode.Ne => CandidateValues.All(c=>!AreEqual(c, filterValues.FirstOrDefault(), FilterItem.ValueType)),
			EFilterOperationMode.Gt => CandidateValues.Any(c=>CompareNumber(c, filterValues.FirstOrDefault()) > 0),
			EFilterOperationMode.Ge => CandidateValues.Any(c=>CompareNumber(c, filterValues.FirstOrDefault()) >= 0),
			EFilterOperationMode.Lt => CandidateValues.Any(c=>CompareNumber(c, filterValues.FirstOrDefault()) < 0),
			EFilterOperationMode.Le => CandidateValues.Any(c=>CompareNumber(c, filterValues.FirstOrDefault()) <= 0),
			_ => true,
		};
	}

	static bool TryGetCoreFieldValues(JnWord Word, str Field, out IReadOnlyList<obj?> Values){
		switch(Field){
			case nameof(PoWord.Head):
				Values = [Word.Head];
				return true;
			case nameof(PoWord.Lang):
				Values = [Word.Lang];
				return true;
			case nameof(PoWord.StoredAt):
				Values = [Word.Word.StoredAt.Value];
				return true;
			case nameof(PoWord.BizCreatedAt):
				Values = [Word.Word.BizCreatedAt.Value];
				return true;
			case nameof(PoWord.BizUpdatedAt):
				Values = [Word.Word.BizUpdatedAt.Value];
				return true;
			default:
				Values = [];
				return false;
		}
	}

	static bool TryGetPropFieldValues(JnWord Word, str Field, out IReadOnlyList<obj?> Values){
		var values = new List<obj?>();
		foreach(var prop in Word.Props){
			if(prop.KStr != Field){
				continue;
			}
			values.Add(GetPropValue(prop));
		}
		Values = values;
		return values.Count > 0;
	}

	static obj? GetPropValue(PoWordProp Prop){
		return Prop.VType switch{
			EKvType.Str => Prop.VStr,
			EKvType.I64 => Prop.VI64,
			EKvType.F64 => Prop.VF64,
			EKvType.Binary => Prop.VBinary,
			_ => null,
		};
	}

	static bool AreEqual(obj? Candidate, obj? Expected, EValueType ValueType){
		if(ValueType == EValueType.Number){
			if(!TryToF64(Candidate, out var cn) || !TryToF64(Expected, out var en)){
				return false;
			}
			return cn == en;
		}
		if(Candidate is null || Expected is null){
			return Candidate is null && Expected is null;
		}
		return string.Equals(Candidate.ToString(), Expected.ToString(), StringComparison.Ordinal);
	}

	static int CompareNumber(obj? Left, obj? Right){
		if(!TryToF64(Left, out var l) || !TryToF64(Right, out var r)){
			return int.MinValue;
		}
		return l.CompareTo(r);
	}

	static bool TryToF64(obj? Value, out f64 Number){
		switch(Value){
			case null:
				Number = default;
				return false;
			case byte v:
				Number = v;
				return true;
			case sbyte v:
				Number = v;
				return true;
			case short v:
				Number = v;
				return true;
			case ushort v:
				Number = v;
				return true;
			case int v:
				Number = v;
				return true;
			case uint v:
				Number = v;
				return true;
			case long v:
				Number = v;
				return true;
			case ulong v:
				Number = v;
				return true;
			case float v:
				Number = v;
				return true;
			case double v:
				Number = v;
				return true;
			case decimal v:
				Number = (double)v;
				return true;
			case Tempus v:
				Number = v.Value;
				return true;
			default:
				if(double.TryParse(Value.ToString(), out var parsed)){
					Number = parsed;
					return true;
				}
				Number = default;
				return false;
		}
	}

	public async Task<nil> BatAddNewLearnRecord(
		IDbUserCtx Ctx
		,IAsyncEnumerable<PoWordLearn> PoWordLearnAsyE, CT Ct
	){
		return await SqlCmdMkr.StartTxnIfNoCtx(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
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
		return SqlCmdMkr.StartTxnIfNoCtx(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
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
		return await SqlCmdMkr.StartTxnIfNoCtx(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
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
