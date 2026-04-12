namespace Ngaq.Local.Domains.Word.Svc;

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
using Ngaq.Local.Db.TswG;
using Ngaq.Local.Word.Dao;
using Tsinswreng.CsErr;
using Tsinswreng.CsCore;
using Tsinswreng.CsSql;
using Tsinswreng.CsTools;
using Ngaq.Core.Tools.Json;
using Ngaq.Core.Shared.Dictionary.Models;
using Ngaq.Core.Shared.Word.Models.Dto;
using Ngaq.Core.Shared.Base.Models.Po;

public partial class SvcWordV2(
	ISqlCmdMkr SqlCmdMkr
	,DaoWordV2 DaoWordV2
	,IRepo<PoWord, IdWord> RepoWord
	,IRepo<PoWordProp, IdWordProp> RepoProp
	,IRepo<PoWordLearn, IdWordLearn> RepoLearn
	,IRepo<PoStudyPlan, IdStudyPlan> RepoStudyPlan
	,IRepo<PoPreFilter, IdPreFilter> RepoPreFilter
	,ISvcStudyPlan SvcStudyPlan
	,ISvcNormLangToUserLang SvcNormLangToUserLang
	,ISvcWordInMem SvcWordInMem
	,IJsonSerializer JsonS
):ISvcWordV2,ISvcWordSync
{
	ISqlCmdMkr SqlCmdMkr = SqlCmdMkr;
	DaoWordV2 DaoWordV2 = DaoWordV2;
	IRepo<PoWord, IdWord> RepoWord = RepoWord;
	IRepo<PoWordProp, IdWordProp> RepoProp = RepoProp;
	IRepo<PoWordLearn, IdWordLearn> RepoLearn = RepoLearn;
	IRepo<PoStudyPlan, IdStudyPlan> RepoStudyPlan = RepoStudyPlan;
	IRepo<PoPreFilter, IdPreFilter> RepoPreFilter = RepoPreFilter;
	ISvcStudyPlan SvcStudyPlan = SvcStudyPlan;
	ISvcNormLangToUserLang SvcNormLangToUserLang = SvcNormLangToUserLang;
	ISvcWordInMem SvcWordInMem = SvcWordInMem;


	public async IAsyncEnumerable<JnWord> GetWordsToLearn(
		IDbUserCtx Ctx, [EnumeratorCancellation] CT Ct
	){
		Ctx.DbFnCtx ??= new DbFnCtx();
		var preFilter = await GetCurStudyPlanPreFilter(Ctx, Ct);
		await foreach(var word in GetWordsToLearn(Ctx, preFilter, Ct).WithCancellation(Ct)){
			yield return word;
		}
	}

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

	async Task<str> ResolveUserLangByNormLang(IDbUserCtx Ctx, ELangIdentType NormLangType, str NormLang, CT Ct){
		Ctx.DbFnCtx ??= new DbFnCtx();
		var UserLang = await SvcNormLangToUserLang.GetUserLangByNormLang(
			Ctx,
			NormLangType,
			NormLang,
			Ct
		);
		if(string.IsNullOrWhiteSpace(UserLang)){
			throw ItemsErr.Word.NormLangToUserLangIsNotMapped
				.ToErr(NormLangType, NormLang);
		}
		return UserLang;
	}

	void AppendDescrProps(IList<PoWordProp> Out, IList<str> Descrs, Tempus BizTime){
		foreach(var Descr in Descrs){
			var Text = (Descr ?? "").Trim();
			if(Text == ""){
				continue;
			}
			Out.Add(MkStrProp(KeysProp.Inst.description, Text, BizTime));
		}
	}

	void AppendPronunciationProps(
		IList<PoWordProp> Out,
		IList<TextedPronunciation> Pronunciations,
		Tempus BizTime
	){
		foreach(var Pron in Pronunciations){
			var TextType = (Pron.TextType ?? "").Trim();
			var Text = (Pron.Text ?? "").Trim();
			if(TextType == "" && Text == ""){
				continue;
			}
			var PronJson = JsonS.Stringify(new TextedPronunciation{
				TextType = TextType,
				Text = Text,
			});
			Out.Add(MkStrProp(KeysProp.Inst.pronunciation, PronJson, BizTime));
		}
	}

	static PoWordProp MkStrProp(str Key, str Value, Tempus BizTime){
		var R = new PoWordProp{
			BizCreatedAt = BizTime,
			BizUpdatedAt = BizTime,
		};
		R.SetStrToken(null, Key, Value);
		return R;
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
				throw ItemsErr.Common.DataIllegalOrConflict.ToErr(e.Message);
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
	public Task<IAsyncEnumerable<IdWord?>> BatUpdPoWord(IDbUserCtx Ctx, IAsyncEnumerable<PoWord> PoWords, CT Ct){
		return SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
			var results = new List<IdWord?>();
			await foreach(var arg in PoWords.WithCancellation(Ct)){
				var old = await DaoWordV2.BatGetPoWordByIdWithDel(DbCtx, ToAsyE([arg.Id]), Ct).FirstOrDefaultAsync(Ct);
				if(old is null){
					throw ItemsErr.Word.WordOfId__NotFound.ToErr(arg.Id);
				}
				old.CheckOwner(Ctx.UserCtx.UserId);

				IdWord finalId = arg.Id;
				if(old.Head != arg.Head || old.Lang != arg.Lang){
					var headLangR = await BatUpdHeadLangCore(DbCtx, Ctx.UserCtx.UserId, ToAsyE([arg]), Ct);
					if(headLangR.Count > 0 && headLangR[0] is IdWord changedId){
						finalId = changedId;
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
				results.Add(finalId == arg.Id ? null : finalId);
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
						throw ItemsErr.Word.WordOfId__NotFound.ToErr(oldId);
					}
					old.CheckOwner(Ctx.UserCtx.UserId);

					var neo = await DaoWordV2.BatGetPoWordByIdWithDel(DbCtx, ToAsyE([newId]), Ct).FirstOrDefaultAsync(Ct);
					if(neo is not null){
						throw ItemsErr.Common.DataIllegalOrConflict.ToErr(oldId, newId);
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
	public async IAsyncEnumerable<IdWord?> BatUpdHeadLang(
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

	async Task<List<IdWord?>> BatUpdHeadLangCore(
		IDbFnCtx DbCtx,
		IdUser UserId,
		IAsyncEnumerable<PoWord> PoWords,
		CT Ct
	){
		var results = new List<IdWord?>();
		await foreach(var arg in PoWords.WithCancellation(Ct)){
			// step 1: 先按 Id 校驗舊詞存在與權限。
			var wordOfId = await DaoWordV2.BatGetPoWordByIdWithDel(DbCtx, ToAsyE([arg.Id]), Ct).FirstOrDefaultAsync(Ct);
			if(wordOfId is null){
				throw ItemsErr.Word.WordOfId__NotFound.ToErr(arg.Id);
			}
			wordOfId.CheckOwner(UserId);

			if(wordOfId.Head == arg.Head && wordOfId.Lang == arg.Lang){
				results.Add(null);
				continue;
			}

			// step 2: 查目標 (Head,Lang) 是否已存在（含軟刪）。
			var target = await DaoWordV2.BatGetPoWordByOwnerHeadLangWithDel(
				DbCtx,
				UserId,
				ToAsyE([new Head_Lang(arg.Head, arg.Lang)]),
				Ct
			).FirstOrDefaultAsync(Ct);

			if(target is null){
				await DaoWordV2.BatUpdHeadLangById(DbCtx, ToAsyE([(wordOfId.Id, arg.Head, arg.Lang)]), Ct);
				results.Add(null);
				continue;
			}

			if(!target.DelAt.IsNullOrDefault()){
				await DaoWordV2.BatRestoreInId(DbCtx, ToAsyE([target.Id]), Ct);
			}

			// step 3: 衝突時，軟刪舊詞，資產改外鍵遷移到目標詞。
			await RepoWord.BatSoftDelById(DbCtx, ToAsyE([wordOfId.Id]), Ct);
			await DaoWordV2.BatMoveAssetsToWordId(DbCtx, ToAsyE([(wordOfId.Id, target.Id)]), Ct);
			await DaoWordV2.BatAltWordAfterUpd(DbCtx, ToAsyE([wordOfId.Id, target.Id]), Ct);
			results.Add(target.Id == arg.Id ? null : target.Id);
		}
		return results;
	}

	/// 先按 BizId(Owner,Head,Lang) 做內存比較，再返回同步 DTO。
	public async IAsyncEnumerable<DtoJnWordSyncResult> BizSyncJnWordByBizId(
		IDbUserCtx Ctx,
		IAsyncEnumerable<JnWord> JnWords,
		[EnumeratorCancellation] CT Ct
	){
		Ctx.DbFnCtx ??= new DbFnCtx();
		await foreach(var remote in JnWords.WithCancellation(Ct)){
			remote.Owner = Ctx.UserCtx.UserId;
			remote.EnsureForeignId();
			var localPo = await DaoWordV2.BatGetPoWordByOwnerHeadLangWithDel(
				Ctx.DbFnCtx,
				Ctx.UserCtx.UserId,
				ToAsyE([new Head_Lang(remote.Head, remote.Lang)]),
				Ct
			).FirstOrDefaultAsync(Ct);
			JnWord? local = null;
			if(localPo is not null){
				local = await DaoWordV2.BatGetJnWordByIdWithDel(Ctx.DbFnCtx, ToAsyE([localPo.Id]), Ct)
					.FirstOrDefaultAsync(Ct);
			}
			yield return SvcWordInMem.SyncJnWord(local, remote);
		}
	}

	/// 根據 DTO 的差異分類，分發到對應處理函數。
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

	/// 無需同步，僅消費輸入保持批量流程一致。
	public async Task<nil> BatSync_NoChange(IDbUserCtx Ctx, IAsyncEnumerable<DtoJnWordSyncResult> Dtos, CT Ct){
		await Dtos.ConsumeAll(Ct);
		return NIL;
	}

	/// 遠端更舊，無需同步，僅消費輸入。
	public async Task<nil> BatSync_RemoteIsOlder(IDbUserCtx Ctx, IAsyncEnumerable<DtoJnWordSyncResult> Dtos, CT Ct){
		await Dtos.ConsumeAll(Ct);
		return NIL;
	}

	/// 本地不存在時，直接批量新增遠端單詞。
	public Task<nil> BatSync_LocalNotExist(IDbUserCtx Ctx, IAsyncEnumerable<DtoJnWordSyncResult> Dtos, CT Ct){
		return SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
			var neos = Dtos.Select(x=>{
				var remote = x.Remote ?? throw ItemsErr.Word.Word__And__SyncFailed.ToErr("RemoteNull");
				remote.Owner = Ctx.UserCtx.UserId;
				remote.EnsureForeignId();
				return remote;
			});
			await RepoWord.BatAddAgg<JnWord>(DbCtx, neos, Ct);
			return NIL;
		});
	}

	/// Id 不同時，以更小 Id 爲準，先統一 Id，再把遠端內容合入。
	public Task<nil> BatSync_IdNotEqual(IDbUserCtx Ctx, IAsyncEnumerable<DtoJnWordSyncResult> Dtos, CT Ct){
		return SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
			await foreach(var dto in Dtos.WithCancellation(Ct)){
				var local = dto.Local ?? throw ItemsErr.Word.Word__And__SyncFailed.ToErr("LocalNull");
				var remote = dto.Remote ?? throw ItemsErr.Word.Word__And__SyncFailed.ToErr("RemoteNull");
				local.Word.CheckOwner(Ctx.UserCtx.UserId);
				remote.Owner = Ctx.UserCtx.UserId;
				remote.EnsureForeignId();

				var keepId = local.Id.Value <= remote.Id.Value ? local.Id : remote.Id;
				if(local.Id != keepId){
					await BatChangeId(
						new DbUserCtx(Ctx.UserCtx, DbCtx),
						ToAsyE([(local.Id, keepId)]),
						Ct
					);
				}

				remote.SetIdEtEnsureFKey(keepId);
				await ApplyRemoteWordAsUpdate(DbCtx, remote, Ct);
			}
			return NIL;
		});
	}

	/// Remote 更新時，把遠端根與資產合入本地。
	Task<nil> BatSync_RemoteIsNewer(IDbUserCtx Ctx, IAsyncEnumerable<DtoJnWordSyncResult> Dtos, CT Ct){
		return SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async(DbCtx)=>{
			await foreach(var dto in Dtos.WithCancellation(Ct)){
				var remote = dto.Remote ?? throw ItemsErr.Word.Word__And__SyncFailed.ToErr("RemoteNull");
				remote.Owner = Ctx.UserCtx.UserId;
				remote.EnsureForeignId();
				await ApplyRemoteWordAsUpdate(DbCtx, remote, Ct);
			}
			return NIL;
		});
	}

	/// 把遠端整詞內容合入到同 Id 的本地詞：根用 Upd，資產用 Upsert。
	async Task<nil> ApplyRemoteWordAsUpdate(IDbFnCtx DbCtx, JnWord Remote, CT Ct){
		await RepoWord.BatUpd(DbCtx, ToAsyE([Remote.Word]), Ct);
		if(Remote.Props.Count > 0){
			await RepoProp.BatUpsert(DbCtx, ToAsyE(Remote.Props), Ct);
		}
		if(Remote.Learns.Count > 0){
			await RepoLearn.BatUpsert(DbCtx, ToAsyE(Remote.Learns), Ct);
		}
		await DaoWordV2.BatAltWordAfterUpd(DbCtx, ToAsyE([Remote.Id]), Ct);
		return NIL;
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
