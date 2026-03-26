namespace Ngaq.Local.Word.Dao;

using System.Runtime.CompilerServices;
using System.Text;
using Ngaq.Core.Infra;
using Ngaq.Core.Infra.IF;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Shared.Base.Models;
using Ngaq.Core.Shared.Base.Models.Po;
using Ngaq.Core.Shared.StudyPlan.Models.PreFilter;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.Word.Models;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Shared.Word.Models.Po.Learn;
using Ngaq.Core.Shared.Word.Models.Po.Word;
using Ngaq.Core.Word.Models.Po.Word;
using Ngaq.Local.Db.TswG;
using Tsinswreng.CsCore;
using Tsinswreng.CsSql;
using Tsinswreng.CsTools;
using IStr_Obj = System.Collections.Generic.IDictionary<str, obj?>;

public partial class DaoWordV2(
	ISqlCmdMkr SqlCmdMkr
	,ITblMgr TblMgr
	,IRepo<PoWord, IdWord> RepoWord
	,IRepo<PoWordProp, IdWordProp> RepoProp
	,IRepo<PoWordLearn, IdWordLearn> RepoLearn
){
	ITable<PoWord> TW => TblMgr.GetTbl<PoWord>();
	ITable<PoWordProp> TP => TblMgr.GetTbl<PoWordProp>();
	ITable<PoWordLearn> TL => TblMgr.GetTbl<PoWordLearn>();
	IRepo<PoWordProp, IdWordProp> RepoProp = RepoProp;
	IRepo<PoWordLearn, IdWordLearn> RepoLearn = RepoLearn;

	public Task<nil> BatAltWordAfterUpd(
		IDbFnCtx Ctx, IAsyncEnumerable<IdWord> Ids, CT Ct
	){
		return RepoWord.AsAppRepo().BatBizTouch(Ctx, Ids, Ct);
	}

	public IAsyncEnumerable<PoWord?> BatGetPoWordByIdWithDel(
		IDbFnCtx Ctx
		,IAsyncEnumerable<IdWord> Ids
		,CT Ct
	){
		return RepoWord.BatGetByIdWithDel(Ctx, Ids, Ct);
	}

	public IAsyncEnumerable<JnWord?> BatGetJnWordByIdWithDel(
		IDbFnCtx Ctx
		,IAsyncEnumerable<IdWord> Ids
		,CT Ct
	){
		return RepoWord.BatGetAggByIdWithDel<JnWord>(Ctx, Ids, Ct);
	}
	

	public IAsyncEnumerable<IdWord?> BatGetIdByOwnerHeadLang(
		IDbFnCtx Ctx
		,IdUser Owner
		,IAsyncEnumerable<Head_Lang> HeadLangs
		,CT Ct
	){
		var Heads = HeadLangs.Select(x=>x.Head);
		var Langs = HeadLangs.Select(x=>x.Lang);
		var Sql = TW.SqlSplicer().Select(x=>x.Id).From().Where1()
		.And(TW.SqlIsNonDel())
		.AndEq(x=>x.Owner, y=>y.One(Owner))
		.AndEq(x=>x.Head, y=>y.Many(Heads))
		.AndEq(x=>x.Lang, y=>y.Many(Langs))
		;
		var Dicts = SqlCmdMkr.RunDupliSql(Ctx, Sql, Ct);
		return Dicts.Select(x=>{
			if(x is null){
				return null;
			}
			var RawId = x[TW.Memb(e=>e.Id)];
			return (IdWord?)IdWord.FromByteArr((u8[])RawId!);
		});
	}

	public IAsyncEnumerable<JnWord> GetWordsByOwner(
		IDbFnCtx Ctx
		,IdUser Owner
		,CT Ct
	){
		var Sql = TW.SqlSplicer().Select("*").From().Where1()
		.And(TW.SqlIsNonDel())
		.AndEq(x=>x.Owner, y=>y.One(Owner))
		.OrderByDesc(x=>x.BizCreatedAt)
		;
		var PoWords = SqlCmdMkr.RunDupliSql(Ctx, TW, Sql, Ct)
		.Where(x=>x is not null)
		.Select(x=>x!)
		;

		return CollectJnWordsByBatch(Ctx, PoWords, Ct);
	}

	public async IAsyncEnumerable<JnWord> GetWordsByOwnerByPreFilterSql(
		IDbFnCtx Ctx,
		IdUser Owner,
		PreFilter? PreFilter,
		[EnumeratorCancellation] CT Ct
	){
		if(PreFilter is null){
			await foreach(var word in GetWordsByOwner(Ctx, Owner, Ct).WithCancellation(Ct)){
				yield return word;
			}
			yield break;
		}

		var candidateIds = await QueryWordIdsByCoreFilter(Ctx, Owner, PreFilter, Ct);
		if(candidateIds.Count == 0){
			yield break;
		}

		var matchedIds = await FilterWordIdsByPropFilter(Ctx, candidateIds, PreFilter, Ct);
		if(matchedIds.Count == 0){
			yield break;
		}

		var words = RepoWord.BatGetAggById<JnWord>(Ctx, ToolAsyE.ToAsyE(matchedIds), Ct);
		await foreach(var word in words.WithCancellation(Ct)){
			if(word is null){
				continue;
			}
			yield return word;
		}
	}

	async Task<List<IdWord>> QueryWordIdsByCoreFilter(
		IDbFnCtx Ctx,
		IdUser Owner,
		PreFilter PreFilter,
		CT Ct
	){
		var where = new StringBuilder("1=1");
		where.Append("\n").Append(TW.SqlIsNonDel());

		var pOwner = TW.Prm(nameof(PoWord.Owner));
		where.Append($"\nAND {TW.QtCol(nameof(PoWord.Owner))} = {pOwner}");
		var arg = ArgDict.Mk(TW).AddT(pOwner, Owner, nameof(PoWord.Owner));

		foreach(var fieldsFilter in PreFilter.CoreFilter){
			var one = BuildCoreFieldsFilterSql(fieldsFilter, arg);
			where.Append("\nAND ").Append(one);
		}

		var sql =
$"""
SELECT {TW.QtCol(nameof(PoWord.Id))}
FROM {TW.Qt(TW.DbTblName)}
WHERE {where}
ORDER BY {TW.QtCol(nameof(PoWord.BizCreatedAt))} DESC
""";
		var cmd = await SqlCmdMkr.Prepare(Ctx, sql, Ct);
		Ctx.AddToDispose(cmd);
		var rows = Ctx.RunCmd(cmd, arg).AsyE1d(Ct);

		var ids = new List<IdWord>();
		await foreach(var row in rows){
			if(!TryGetWordId(row, out var id)){
				continue;
			}
			ids.Add(id);
		}
		return ids;
	}

	async Task<List<IdWord>> FilterWordIdsByPropFilter(
		IDbFnCtx Ctx,
		IReadOnlyList<IdWord> CandidateIds,
		PreFilter PreFilter,
		CT Ct
	){
		if(PreFilter.PropFilter.Count == 0){
			return CandidateIds.ToList();
		}
		if(CandidateIds.Count == 0){
			return [];
		}

		var opt = new OptQry{
			InParamCnt = (u64)CandidateIds.Count,
			IncludeDeleted = false,
		};
		var propsById = await RepoWord.IncludeEntitysByKeys(
			Ctx,
			nameof(I_WordId.WordId),
			opt,
			CandidateIds,
			x=>x.WordId,
			TP,
			Ct
		);

		var r = new List<IdWord>(CandidateIds.Count);
		foreach(var id in CandidateIds){
			var props = propsById.GetValueOrDefault(id, []);
			if(!IsMatchedByPropFilters(props, PreFilter.PropFilter)){
				continue;
			}
			r.Add(id);
		}
		return r;
	}

	str BuildCoreFieldsFilterSql(FieldsFilter FieldsFilter, IArgDict Arg){
		if(FieldsFilter.Fields.Count == 0){
			return "(1=1)";
		}
		if(FieldsFilter.Filters.Count == 0){
			return "(1=1)";
		}

		var fieldExprs = new List<str>();
		foreach(var field in FieldsFilter.Fields){
			var fieldExpr = BuildOneCoreFieldExpr(field, FieldsFilter.Filters, Arg);
			if(string.IsNullOrWhiteSpace(fieldExpr)){
				continue;
			}
			fieldExprs.Add(fieldExpr);
		}

		if(fieldExprs.Count == 0){
			return "(0=1)";
		}
		return "(" + str.Join(" OR ", fieldExprs) + ")";
	}

	str BuildOneCoreFieldExpr(str Field, IList<FilterItem> Filters, IArgDict Arg){
		if(!TryMapCoreFieldToWordCodeCol(Field, out var codeCol)){
			return "";
		}
		var conds = new List<str>(Filters.Count);
		foreach(var filter in Filters){
			conds.Add(BuildCoreFilterItemCondition(codeCol, filter, Arg));
		}
		if(conds.Count == 0){
			return "(1=1)";
		}
		return "(" + str.Join(" AND ", conds) + ")";
	}

	str BuildCoreFilterItemCondition(str CodeCol, FilterItem Filter, IArgDict Arg){
		var col = TW.QtCol(CodeCol);
		var vals = Filter.Values ?? [];

		str Eq(obj? v){
			if(v is null){
				return $"{col} IS NULL";
			}
			var p = TW.Prm();
			Arg.AddT(p, NormalizeCoreFilterValue(CodeCol, v), CodeCol);
			return $"{col} = {p}";
		}
		str Ne(obj? v){
			if(v is null){
				return $"{col} IS NOT NULL";
			}
			var p = TW.Prm();
			Arg.AddT(p, NormalizeCoreFilterValue(CodeCol, v), CodeCol);
			return $"{col} != {p}";
		}

		switch(Filter.Operation){
			case EFilterOperationMode.IncludeAny:{
				if(vals.Count == 0){
					return "(0=1)";
				}
				return "(" + str.Join(" OR ", vals.Select(Eq)) + ")";
			}
			case EFilterOperationMode.IncludeAll:{
				if(vals.Count == 0){
					return "(1=1)";
				}
				return "(" + str.Join(" AND ", vals.Select(Eq)) + ")";
			}
			case EFilterOperationMode.ExcludeAll:{
				if(vals.Count == 0){
					return "(1=1)";
				}
				return "(" + str.Join(" AND ", vals.Select(Ne)) + ")";
			}
			case EFilterOperationMode.Eq:
				return "(" + Eq(vals.FirstOrDefault()) + ")";
			case EFilterOperationMode.Ne:
				return "(" + Ne(vals.FirstOrDefault()) + ")";
			case EFilterOperationMode.Gt:
				return BuildCoreCmpCondition(col, CodeCol, vals.FirstOrDefault(), ">", Arg);
			case EFilterOperationMode.Ge:
				return BuildCoreCmpCondition(col, CodeCol, vals.FirstOrDefault(), ">=", Arg);
			case EFilterOperationMode.Lt:
				return BuildCoreCmpCondition(col, CodeCol, vals.FirstOrDefault(), "<", Arg);
			case EFilterOperationMode.Le:
				return BuildCoreCmpCondition(col, CodeCol, vals.FirstOrDefault(), "<=", Arg);
			default:
				return "(1=1)";
		}
	}

	str BuildCoreCmpCondition(
		str QtCol,
		str CodeCol,
		obj? V,
		str Op,
		IArgDict Arg
	){
		if(V is null){
			return "(0=1)";
		}
		var p = TW.Prm();
		Arg.AddT(p, NormalizeCoreFilterValue(CodeCol, V), CodeCol);
		return $"({QtCol} {Op} {p})";
	}

	obj? NormalizeCoreFilterValue(str CodeCol, obj? Raw){
		if(Raw is null){
			return null;
		}
		switch(CodeCol){
			case nameof(PoWord.Head):
			case nameof(PoWord.Lang):
				return Raw.ToString();
			case nameof(PoWord.StoredAt):
			case nameof(PoWord.BizCreatedAt):
			case nameof(PoWord.BizUpdatedAt):
				if(Raw is Tempus t){
					return t;
				}
				if(Raw is DateTime dt){
					return Tempus.FromDateTime(dt);
				}
				if(i64.TryParse(Raw.ToString(), out var ms)){
					return new Tempus(ms);
				}
				if(Tempus.TryFromIso(Raw.ToString()??"", out var parsed)){
					return parsed;
				}
				return Tempus.Zero;
			default:
				return Raw;
		}
	}

	bool TryMapCoreFieldToWordCodeCol(str Field, out str CodeCol){
		CodeCol = Field;
		switch(Field){
			case nameof(PoWord.Head):
			case nameof(PoWord.Lang):
			case nameof(PoWord.StoredAt):
			case nameof(PoWord.BizCreatedAt):
			case nameof(PoWord.BizUpdatedAt):
				return true;
			default:
				CodeCol = "";
				return false;
		}
	}

	bool TryGetWordId(IStr_Obj Raw, out IdWord Id){
		var dbCol = TW.DbCol(nameof(PoWord.Id));
		if(!Raw.TryGetValue(dbCol, out var rawV) && !Raw.TryGetValue(nameof(PoWord.Id), out rawV)){
			Id = default;
			return false;
		}
		var upper = TW.RawToUpper(rawV, nameof(PoWord.Id));
		if(upper is IdWord id){
			Id = id;
			return true;
		}
		Id = default;
		return false;
	}

	static bool IsMatchedByPropFilters(IList<PoWordProp> Props, IList<FieldsFilter> FieldFilters){
		foreach(var ff in FieldFilters){
			if(IsMatchedByOnePropFieldsFilter(Props, ff)){
				continue;
			}
			return false;
		}
		return true;
	}

	static bool IsMatchedByOnePropFieldsFilter(IList<PoWordProp> Props, FieldsFilter FieldFilter){
		if(FieldFilter.Fields.Count == 0){
			return true;
		}
		foreach(var field in FieldFilter.Fields){
			var values = Props.Where(x=>x.KStr == field).Select(GetPropValue).ToList();
			if(values.Count == 0){
				continue;
			}
			if(IsMatchedByPropFilterItems(values, FieldFilter.Filters)){
				return true;
			}
		}
		return false;
	}

	static bool IsMatchedByPropFilterItems(IReadOnlyList<obj?> CandidateValues, IList<FilterItem> FilterItems){
		foreach(var f in FilterItems){
			if(IsMatchedByOnePropFilterItem(CandidateValues, f)){
				continue;
			}
			return false;
		}
		return true;
	}

	static bool IsMatchedByOnePropFilterItem(IReadOnlyList<obj?> CandidateValues, FilterItem FilterItem){
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

	IAsyncEnumerable<JnWord> CollectJnWordsByBatch(
		IDbFnCtx Ctx
		,IAsyncEnumerable<PoWord> PoWords
		,CT Ct
	){
		var Batch = new BatchCollector<PoWord, IList<JnWord>>(async(words, Ct)=>{
			var Ids = words.Select(x=>x.Id).ToList();
			if(Ids.Count == 0){
				return [];
			}
			var NWordId = nameof(I_WordId.WordId);
			var Opt = new OptQry{
				InParamCnt = (u64)Ids.Count,
				IncludeDeleted = false,
			};
			var PropsById = await RepoWord.IncludeEntitysByKeys(Ctx, NWordId, Opt, Ids, x=>x.WordId, TP, Ct);
			var LearnsById = await RepoWord.IncludeEntitysByKeys(Ctx, NWordId, Opt, Ids, x=>x.WordId, TL, Ct);

			IList<JnWord> R = [];
			foreach(var PoWord in words){
				var Props = PropsById.GetValueOrDefault(PoWord.Id, []);
				var Learns = LearnsById.GetValueOrDefault(PoWord.Id, []);
				R.Add(new JnWord(PoWord, Props, Learns));
			}
			return R;
		});
		var S2D = Batch.AddToEnd(PoWords, Ct);
		return S2D.Flat();
	}
}
