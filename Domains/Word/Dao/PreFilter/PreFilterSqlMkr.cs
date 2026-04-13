using System.Text;
using Ngaq.Core.Infra;
using Ngaq.Core.Shared.StudyPlan.Models.PreFilter;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.Word.Models.Po.Word;
using Ngaq.Local.Db.TswG;
using Tsinswreng.CsSql;
using Tsinswreng.CsTempus;
using StudyPreFilter = Ngaq.Core.Shared.StudyPlan.Models.PreFilter.PreFilter;
namespace Ngaq.Local.Word.Dao;

public sealed class PreFilterSqlMkr{
	i32 _prmSeq = 0;
	/// 將 Word CoreFilter 轉爲 SQL where 片段與參數。
	/// </summary>
	/// <param name="tblWord">PoWord 對應表。</param>
	/// <param name="owner">當前用戶。</param>
	/// <param name="preFilter">前置篩選器，可爲 null。</param>
	/// <returns>
	/// whereSql: 可直接拼到 WHERE 後；
	/// arg: 對應參數。
	/// </returns>
	public CoreFilterSql BuildCoreFilterWhere(
		ITable<PoWord> tblWord,
		IdUser owner,
		StudyPreFilter? preFilter
	){
		_prmSeq = 0;
		var where = new StringBuilder("1=1");
		where.Append("\nAND ").Append(tblWord.SqlIsNonDel());

		var pOwner = tblWord.Prm(nameof(PoWord.Owner));
		where.Append($"\nAND {tblWord.QtCol(nameof(PoWord.Owner))} = {pOwner}");
		var arg = ArgDict.Mk(tblWord).AddT(pOwner, owner, nameof(PoWord.Owner));

		if(preFilter is null || preFilter.CoreFilter.Count == 0){
			return new CoreFilterSql(where.ToString(), arg);
		}

		foreach(var fieldsFilter in preFilter.CoreFilter){
			var one = BuildCoreFieldsFilterSql(tblWord, fieldsFilter, arg);
			where.Append("\nAND ").Append(one);
		}
		return new CoreFilterSql(where.ToString(), arg);
	}

	str BuildCoreFieldsFilterSql(ITable<PoWord> tblWord, FieldsFilter fieldsFilter, IArgDict arg){
		if(fieldsFilter.Fields.Count == 0 || fieldsFilter.Filters.Count == 0){
			return "(1=1)";
		}

		var fieldExprs = new List<str>();
		foreach(var field in fieldsFilter.Fields){
			var fieldExpr = BuildOneCoreFieldExpr(tblWord, field, fieldsFilter.Filters, arg);
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

	str BuildOneCoreFieldExpr(
		ITable<PoWord> tblWord,
		str field,
		IList<FilterItem> filters,
		IArgDict arg
	){
		if(!TryMapCoreFieldToWordCodeCol(field, out var codeCol)){
			return "";
		}
		var conds = new List<str>(filters.Count);
		foreach(var filter in filters){
			conds.Add(BuildCoreFilterItemCondition(tblWord, codeCol, filter, arg));
		}
		if(conds.Count == 0){
			return "(1=1)";
		}
		return "(" + str.Join(" AND ", conds) + ")";
	}

	str BuildCoreFilterItemCondition(
		ITable<PoWord> tblWord,
		str codeCol,
		FilterItem filter,
		IArgDict arg
	){
		var col = tblWord.QtCol(codeCol);
		var vals = filter.Values ?? [];

		str Eq(obj? v){
			if(v is null){
				return $"{col} IS NULL";
			}
			var p = tblWord.Prm($"CF_{codeCol}_{_prmSeq++}");
			arg.AddT(p, NormalizeCoreFilterValue(codeCol, v), codeCol);
			return $"{col} = {p}";
		}

		str Ne(obj? v){
			if(v is null){
				return $"{col} IS NOT NULL";
			}
			var p = tblWord.Prm($"CF_{codeCol}_{_prmSeq++}");
			arg.AddT(p, NormalizeCoreFilterValue(codeCol, v), codeCol);
			return $"{col} != {p}";
		}

		switch(filter.Operation){
			case EFilterOperationMode.IncludeAny:
				if(vals.Count == 0){
					return "(0=1)";
				}
				return "(" + str.Join(" OR ", vals.Select(Eq)) + ")";
			case EFilterOperationMode.IncludeAll:
				if(vals.Count == 0){
					return "(1=1)";
				}
				return "(" + str.Join(" AND ", vals.Select(Eq)) + ")";
			case EFilterOperationMode.ExcludeAll:
				if(vals.Count == 0){
					return "(1=1)";
				}
				return "(" + str.Join(" AND ", vals.Select(Ne)) + ")";
			case EFilterOperationMode.Eq:
				return "(" + Eq(vals.FirstOrDefault()) + ")";
			case EFilterOperationMode.Ne:
				return "(" + Ne(vals.FirstOrDefault()) + ")";
			case EFilterOperationMode.Gt:
				return BuildCoreCmpCondition(tblWord, col, codeCol, vals.FirstOrDefault(), ">", arg);
			case EFilterOperationMode.Ge:
				return BuildCoreCmpCondition(tblWord, col, codeCol, vals.FirstOrDefault(), ">=", arg);
			case EFilterOperationMode.Lt:
				return BuildCoreCmpCondition(tblWord, col, codeCol, vals.FirstOrDefault(), "<", arg);
			case EFilterOperationMode.Le:
				return BuildCoreCmpCondition(tblWord, col, codeCol, vals.FirstOrDefault(), "<=", arg);
			default:
				return "(1=1)";
		}
	}

	str BuildCoreCmpCondition(
		ITable<PoWord> tblWord,
		str qtCol,
		str codeCol,
		obj? v,
		str op,
		IArgDict arg
	){
		if(v is null){
			return "(0=1)";
		}
		var p = tblWord.Prm($"CF_{codeCol}_{_prmSeq++}");
		arg.AddT(p, NormalizeCoreFilterValue(codeCol, v), codeCol);
		return $"({qtCol} {op} {p})";
	}

	static obj? NormalizeCoreFilterValue(str codeCol, obj? raw){
		if(raw is null){
			return null;
		}
		switch(codeCol){
			case nameof(PoWord.Head):
			case nameof(PoWord.Lang):
				return raw.ToString();
			case nameof(PoWord.StoredAt):
			case nameof(PoWord.BizCreatedAt):
			case nameof(PoWord.BizUpdatedAt):
				if(raw is Tempus t){
					return t;
				}
				if(raw is DateTime dt){
					return Tempus.FromDateTime(dt);
				}
				if(i64.TryParse(raw.ToString(), out var ms)){
					return new Tempus(ms);
				}
				if(Tempus.TryFromIso(raw.ToString()??"", out var parsed)){
					return parsed;
				}
				return Tempus.Zero;
			default:
				return raw;
		}
	}

	static bool TryMapCoreFieldToWordCodeCol(str field, out str codeCol){
		codeCol = field;
		switch(field){
			case nameof(PoWord.Head):
			case nameof(PoWord.Lang):
			case nameof(PoWord.StoredAt):
			case nameof(PoWord.BizCreatedAt):
			case nameof(PoWord.BizUpdatedAt):
				return true;
			default:
				codeCol = "";
				return false;
		}
	}
}

public sealed record CoreFilterSql(str WhereSql, IArgDict Arg);
