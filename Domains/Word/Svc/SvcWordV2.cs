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
		IDbUserCtx Ctx, PreFilter? Prefilter, CT Ct
	){
		Ctx.DbFnCtx ??= new DbFnCtx();
		var words = DaoWordV2.GetWordsByOwnerByPreFilterSql(Ctx.DbFnCtx, Ctx.UserCtx.UserId, Prefilter, Ct);
		return words;
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
			EFilterOperationMode.IncludeAny => filterValues.Any(v=>CandidateValues.Any(c=>WordFilterValueUtil.AreEqual(c, v, FilterItem.ValueType))),
			EFilterOperationMode.IncludeAll => filterValues.All(v=>CandidateValues.Any(c=>WordFilterValueUtil.AreEqual(c, v, FilterItem.ValueType))),
			EFilterOperationMode.ExcludeAll => filterValues.All(v=>CandidateValues.All(c=>!WordFilterValueUtil.AreEqual(c, v, FilterItem.ValueType))),
			EFilterOperationMode.Eq => CandidateValues.Any(c=>WordFilterValueUtil.AreEqual(c, filterValues.FirstOrDefault(), FilterItem.ValueType)),
			EFilterOperationMode.Ne => CandidateValues.All(c=>!WordFilterValueUtil.AreEqual(c, filterValues.FirstOrDefault(), FilterItem.ValueType)),
			EFilterOperationMode.Gt => CandidateValues.Any(c=>WordFilterValueUtil.CompareNumber(c, filterValues.FirstOrDefault()) > 0),
			EFilterOperationMode.Ge => CandidateValues.Any(c=>WordFilterValueUtil.CompareNumber(c, filterValues.FirstOrDefault()) >= 0),
			EFilterOperationMode.Lt => CandidateValues.Any(c=>WordFilterValueUtil.CompareNumber(c, filterValues.FirstOrDefault()) < 0),
			EFilterOperationMode.Le => CandidateValues.Any(c=>WordFilterValueUtil.CompareNumber(c, filterValues.FirstOrDefault()) <= 0),
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
			values.Add(WordFilterValueUtil.GetPropValue(prop));
		}
		Values = values;
		return values.Count > 0;
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

	static PoWordLearn MkAddLearn(IdWord WordId, UnixMs BizCreatedAt){
		return new PoWordLearn{
			WordId = WordId,
			LearnResult = ELearn.Add,
			BizCreatedAt = BizCreatedAt,
		};
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

	static PoWordProp MkStrProp(str Key, str Value, UnixMs BizTime){
		var R = new PoWordProp{
			BizCreatedAt = BizTime,
			BizUpdatedAt = BizTime,
		};
		R.SetStrToken(null, Key, Value);
		return R;
	}

	void AppendDescrProps(IList<PoWordProp> Out, IList<str> Descrs, UnixMs BizTime){
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
		UnixMs BizTime
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

	async Task<str> ResolveUserLangByNormLang(IDbUserCtx Ctx, ELangIdentType NormLangType, str NormLang, CT Ct){
		Ctx.DbFnCtx ??= new DbFnCtx();
		var UserLang = await SvcNormLangToUserLang.GetUserLangByNormLang(
			Ctx,
			NormLangType,
			NormLang,
			Ct
		);
		if(string.IsNullOrWhiteSpace(UserLang)){
			throw KeysErr.Word.NormLangToUserLangIsNotMapped
				.ToErr(NormLangType, NormLang);
		}
		return UserLang;
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
