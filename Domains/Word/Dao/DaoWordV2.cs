namespace Ngaq.Local.Word.Dao;

using System.Runtime.CompilerServices;
using Ngaq.Core.Infra;
using Ngaq.Core.Infra.IF;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Shared.Base.Models;
using Ngaq.Core.Shared.Base.Models.Po;
using Ngaq.Core.Shared.StudyPlan.Models.PreFilter;
using Ngaq.Core.Shared.User.Models.Po;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.Word.Models;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Shared.Word.Models.Po.Learn;
using Ngaq.Core.Shared.Word.Models.Po.Word;
using Ngaq.Core.Word.Models.Po.Word;
using Ngaq.Local.Db.TswG;
using Ngaq.Local.Domains.Word;
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
	PreFilterSqlMkr PreFilterSqlMkr = new();

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
	
	/// 批量按 (Owner, Head, Lang) 查單詞（含已軟刪除），返回順序與入參一一對應。
	public IAsyncEnumerable<PoWord?> BatGetPoWordByOwnerHeadLangWithDel(
		IDbFnCtx Ctx,
		IdUser Owner,
		IAsyncEnumerable<Head_Lang> HeadLangs,
		CT Ct
	){
		var heads = HeadLangs.Select(x=>x.Head);
		var langs = HeadLangs.Select(x=>x.Lang);
		var sql = TW.SqlSplicer().Select("*").From().Where1()
			.AndEq(x=>x.Owner, y=>y.One(Owner))
			.AndEq(x=>x.Head, y=>y.Many(heads))
			.AndEq(x=>x.Lang, y=>y.Many(langs))
		;
		return SqlCmdMkr.RunDupliSql(Ctx, TW, sql, Ct);
	}

	/// 批量更新單詞主體的 Head/Lang，並同步觸發 BizUpdatedAt。
	public async Task<nil> BatUpdHeadLangById(
		IDbFnCtx Ctx,
		IAsyncEnumerable<(IdWord Id, str Head, str Lang)> Args,
		CT Ct
	){
		await using var batch = new BatchCollector<(IdWord Id, str Head, str Lang), nil>(async(rows, Ct)=>{
			if(rows.Count == 0){
				return NIL;
			}
			var ids = new List<IdWord>(rows.Count);
			var dicts = new List<IDictionary<str, obj?>>(rows.Count);
			foreach(var row in rows){
				ids.Add(row.Id);
				dicts.Add(new Dictionary<str, obj?>{
					[nameof(PoWord.Head)] = row.Head,
					[nameof(PoWord.Lang)] = row.Lang,
				});
			}
			await RepoWord.BatUpdByCodeDict(Ctx, ToolAsyE.ToAsyE(ids), ToolAsyE.ToAsyE(dicts), Ct);
			await BatAltWordAfterUpd(Ctx, ToolAsyE.ToAsyE(ids), Ct);
			return NIL;
		});
		await batch.ConsumeAll(Args, Ct);
		return NIL;
	}

	/// 批量取消軟刪除（DelAt = 0），並同步觸發 BizUpdatedAt。
	public async Task<nil> BatRestoreInId(
		IDbFnCtx Ctx,
		IAsyncEnumerable<IdWord> Ids,
		CT Ct
	){
		await using var batch = new BatchCollector<IdWord, nil>(async(idBatch, Ct)=>{
			if(idBatch.Count == 0){
				return NIL;
			}
			var codeDicts = idBatch.Select(_=>new Dictionary<str, obj?>{
				[nameof(IPoBase.DelAt)] = default(IdDel),
			});
			await RepoWord.BatUpdByCodeDict(Ctx, ToolAsyE.ToAsyE(idBatch), ToolAsyE.ToAsyE(codeDicts), Ct);
			await BatAltWordAfterUpd(Ctx, ToolAsyE.ToAsyE(idBatch), Ct);
			return NIL;
		});
		await batch.ConsumeAll(Ids, Ct);
		return NIL;
	}

	/// 批量把資產 (WordProp/WordLearn) 的 WordId 從 Old 移到 New。
	public async Task<nil> BatMoveAssetsToWordId(
		IDbFnCtx Ctx,
		IAsyncEnumerable<(IdWord Old, IdWord New)> Moves,
		CT Ct
	){
		await using var batch = new BatchCollector<(IdWord Old, IdWord New), nil>(async(moveBatch, Ct)=>{
			if(moveBatch.Count == 0){
				return NIL;
			}
			var oldIds = moveBatch.Select(x=>x.Old).Distinct().ToList();
			var opt = new OptQry{
				InParamCnt = (u64)oldIds.Count,
				IncludeDeleted = true,
			};
			var propsByOld = await RepoWord.IncludeEntitysByKeys(
				Ctx,
				nameof(I_WordId.WordId),
				opt,
				oldIds,
				x=>x.WordId,
				TP,
				Ct
			);
			var learnsByOld = await RepoWord.IncludeEntitysByKeys(
				Ctx,
				nameof(I_WordId.WordId),
				opt,
				oldIds,
				x=>x.WordId,
				TL,
				Ct
			);

			var updProps = new List<PoWordProp>();
			var updLearns = new List<PoWordLearn>();
			foreach(var (oldId, newId) in moveBatch){
				foreach(var prop in propsByOld.GetValueOrDefault(oldId, [])){
					prop.WordId = newId;
					updProps.Add(prop);
				}
				foreach(var learn in learnsByOld.GetValueOrDefault(oldId, [])){
					learn.WordId = newId;
					updLearns.Add(learn);
				}
			}
			if(updProps.Count > 0){
				await RepoProp.BatUpd(Ctx, ToolAsyE.ToAsyE(updProps), Ct);
			}
			if(updLearns.Count > 0){
				await RepoLearn.BatUpd(Ctx, ToolAsyE.ToAsyE(updLearns), Ct);
			}
			return NIL;
		});
		await batch.ConsumeAll(Moves, Ct);
		return NIL;
	}

	/// 批量更改 Word 主鍵 Id，並同步把全部資產外鍵一起遷移。
	public async Task<nil> BatChangeWordId(
		IDbFnCtx Ctx,
		IAsyncEnumerable<(IdWord Old, IdWord New)> Moves,
		CT Ct
	){
		await using var batch = new BatchCollector<(IdWord Old, IdWord New), nil>(async(moveBatch, Ct)=>{
			var normalized = moveBatch.Where(x=>x.Old != x.New).ToList();
			if(normalized.Count == 0){
				return NIL;
			}
			var oldIds = normalized.Select(x=>x.Old).ToList();
			var codeDicts = normalized.Select(x=>(IDictionary<str, obj?>)new Dictionary<str, obj?>{
				[nameof(PoWord.Id)] = x.New,
			});
			await RepoWord.BatUpdByCodeDict(Ctx, ToolAsyE.ToAsyE(oldIds), ToolAsyE.ToAsyE(codeDicts), Ct);
			await BatMoveAssetsToWordId(Ctx, ToolAsyE.ToAsyE(normalized), Ct);
			await BatAltWordAfterUpd(Ctx, ToolAsyE.ToAsyE(normalized.Select(x=>x.New)), Ct);
			return NIL;
		});
		await batch.ConsumeAll(Moves, Ct);
		return NIL;
	}

	public IAsyncEnumerable<JnWord> GetWordsByOwner(
		IDbFnCtx Ctx
		,IdUser Owner
		,CT Ct
	){
		var Sql = TW.SqlSplicer().Select("*").From().WhereNonDel()
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
		var coreFilterSql = PreFilterSqlMkr.BuildCoreFilterWhere(TW, Owner, PreFilter);

		var sql =
$"""
SELECT {TW.QtCol(nameof(PoWord.Id))}
FROM {TW.Qt(TW.DbTblName)}
WHERE {coreFilterSql.WhereSql}
ORDER BY {TW.QtCol(nameof(PoWord.BizCreatedAt))} DESC
""";
		var cmd = await SqlCmdMkr.Prepare(Ctx, sql, Ct);
		Ctx.AddToDispose(cmd);
		var rows = Ctx.RunCmd(cmd, coreFilterSql.Arg).AsyE1d(Ct);

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
			var values = Props.Where(x=>x.KStr == field).Select(WordFilterValueUtil.GetPropValue).ToList();
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
