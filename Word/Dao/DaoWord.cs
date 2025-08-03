using Ngaq.Core.Infra;
using Ngaq.Core.Model.Po;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Local.Db;
using Tsinswreng.CsSqlHelper;
using Str_Any = System.Collections.Generic.Dictionary<string, object?>;
using IStr_Any = System.Collections.Generic.IDictionary<string, object?>;
using Ngaq.Core.Infra.Errors;
using Ngaq.Core.Models.Po;
using Ngaq.Core.Word.Models.Po.Learn;
using Tsinswreng.CsPage;
using Ngaq.Core.Word.Models;
using Ngaq.Core.Models.UserCtx;
using Tsinswreng.CsTools;
using Ngaq.Local.Db.TswG;

namespace Ngaq.Local.Word.Dao;


public  partial class DaoSqlWord(
	ISqlCmdMkr SqlCmdMkr
	,ITblMgr TblMgr
	,IAppRepo<PoWord, IdWord> RepoWord
	,IAppRepo<PoWordProp, IdWordProp> RepoKv
	,IAppRepo<PoWordLearn, IdLearn> RepoLearn
){

	public async Task<Func<
		IUserCtx
		,str
		,str
		,CT
		,Task<IdWord?>
	>>
	FnSelectIdByHeadEtLang(
		IDbFnCtx Ctx
		,CT Ct
	){
		var T = TblMgr.GetTable<PoWord>();
		var F = TblMgr.SqlMkr;
		var Sql =
$"""
SELECT {T.Fld(nameof(I_Id<nil>.Id))} FROM {T.Qt(T.DbTblName)}
WHERE {T.Fld(nameof(PoWord.Owner))} = {F.Prm(nameof(PoWord.Owner))}
AND {T.Fld(nameof(PoWord.Head))} = {F.Prm(nameof(PoWord.Head))}
AND {T.Fld(nameof(PoWord.Lang))} = {F.Prm(nameof(PoWord.Lang))}
""";
		var SqlCmd = await SqlCmdMkr.Prepare(Ctx, Sql, Ct);

/*
SELECT Id FROM Word
WHERE Owner = @Owner
AND WordFormId = @WordFormId
AND Lang = @Lang
 */

		return async (
OperatorCtx
			,Head
			,Lang
			,ct
		)=>{
			var UserId = OperatorCtx.UserId;
			var Params = new Str_Any {
				[nameof(PoWord.Owner)] = UserId
				,[nameof(PoWord.Head)] = Head
				,[nameof(PoWord.Lang)] = Lang
			};
//TODO 檢查有無漏T.ToDbDict(Params)者;  多表聯合查詢旹 以此構建參數dict不好用
			var GotDict = await SqlCmd.Args(T.ToDbDict(Params)).Run(ct).FirstOrDefaultAsync(ct);
			if(GotDict == null){
				return null;
			}
			var ans = GotDict[T.ToDbName(nameof(I_Id<nil>.Id))];
			return IdWord.FromByteArr((u8[])ans);
		};
	}


/// <summary>
///
/// </summary>
/// <param name="ct"></param>
/// <returns></returns>
	public async Task<Func<
		IdWord
		,CT
		,Task<JnWord?>
	>> FnSelectJnWordById(
		IDbFnCtx? Ctx
		,CT ct
	){
		var TW = TblMgr.GetTable<PoWord>();
		var TK = TblMgr.GetTable<PoWordProp>();
		var TL = TblMgr.GetTable<PoWordLearn>();
		var NWordId = nameof(PoWordProp.WordId);
		var Sql_SeekByFKey = (str QuotedTblName)=>{
			var Sql =
$"""
SELECT * FROM {QuotedTblName}
WHERE {TK.Fld(NWordId)} = {TW.Prm(NWordId)}
""";
			return Sql;
		};
		var GetPoWordById = await RepoWord.FnSelectById(Ctx, ct);
		var Cmd_SeekKv = await SqlCmdMkr.Prepare(Ctx, Sql_SeekByFKey(TK.Qt(TK.DbTblName)), ct);
		var Cmd_SeekLearn = await SqlCmdMkr.Prepare(Ctx, Sql_SeekByFKey(TL.Qt(TL.DbTblName)), ct);

		var Fn = async(
			IdWord Id
			,CT ct
		)=>{
			var Po_Word = await GetPoWordById(Id, ct);
			if(Po_Word == null){
				return null;
			}
			var Arg = new Str_Any{
				[nameof(PoWordProp.WordId)] = Id
			};
			var RawPropDicts = await Cmd_SeekKv.Args(TK.ToDbDict(Arg)).Run(ct)
				.Select(dbDict=>TK.DbDictToPo<PoWordProp>(dbDict))
				.ToListAsync(ct)
			;
			var RawLearnDicts = await Cmd_SeekLearn.Args(TL.ToDbDict(Arg)).Run(ct)
				.Select(dbDict=>TL.DbDictToPo<PoWordLearn>(dbDict))
				.ToListAsync(ct)
			;
			var ans = new JnWord{
				PoWord = Po_Word
				,Props = RawPropDicts
				,Learns = RawLearnDicts
			};
			return ans;
		};
		return Fn;
	}

	public async Task<Func<
		IEnumerable<JnWord>
		,CT
		,Task<nil>
	>> FnInsertJnWords(
		IDbFnCtx? Ctx
		,CT ct
	) {
		var InsertPoWords = await RepoWord.FnInsertMany(Ctx, ct);
		var InsertPoKvs = await RepoKv.FnInsertMany(Ctx, ct);
		var InsertPoLearns = await RepoLearn.FnInsertMany(Ctx, ct);

		var Fn = async(
			IEnumerable<JnWord> JnWords
			,CT ct
		)=>{
			u64 BatchSize = 0xfff;
			await using var PoWords = new BatchListAsy<PoWord, nil>(async(list, ct)=>{
				await InsertPoWords(list,ct);
				return NIL;
			}, BatchSize);
			await using var PoKvs = new BatchListAsy<PoWordProp, nil>(async(e, ct)=>{
				await InsertPoKvs(e, ct);
				return NIL;
			}, BatchSize);
			await using var PoLearns = new BatchListAsy<PoWordLearn, nil>(async(e, ct)=>{
				await InsertPoLearns(e, ct);
				return NIL;
			}, BatchSize);
			foreach (var JWord in JnWords) {
				JWord.AssignId();
				await PoWords.Add(JWord.PoWord, ct);
				foreach (var Prop in JWord.Props) {
					await PoKvs.Add(Prop, ct);
				}
				foreach (var Learn in JWord.Learns) {
					await PoLearns.Add(Learn, ct);
				}
			}
			await PoWords.End(ct);
			await PoKvs.End(ct);
			await PoLearns.End(ct);
			return NIL;
		};
		return Fn;
	}

	public async Task<Func<
		IEnumerable<PoWordProp>
		,CT
		,Task<nil>
	>> FnInsertPoKvs(
		IDbFnCtx? Ctx
		,CT ct
	){
		var InsertMany = await RepoKv.FnInsertMany(Ctx, ct);
		var Fn = async(
			IEnumerable<PoWordProp> PoKvs
			,CT ct
		)=>{
			PoKvs = PoKvs.Select(x=>{
				if(x.WordId == null || x.WordId.Value == 0){
					throw new ErrArg("PoKv.WordId should not be null or 0.");
				}
				return x;
			});
			await InsertMany(PoKvs, ct);
			return NIL;
		};
		return Fn;
	}

	public async Task<Func<
		IEnumerable<PoWordLearn>
		,CT
		,Task<nil>
	>> FnInsertPoLearns(
		IDbFnCtx? Ctx
		,CT ct
	){
		var InsertMany = await RepoLearn.FnInsertMany(Ctx, ct);
		var Fn = async(
			IEnumerable<PoWordLearn> PoLearns
			,CT ct
		)=>{
			await InsertMany(PoLearns, ct);
			return NIL;
		};
		return Fn;
	}

	public async Task<Func<
		IdWord
		,IPageQuery
		,CT
		,Task<IPageAsy<IStr_Any>>
	>> FnPageByFKey(
		IDbFnCtx Ctx
		,ITable Tbl
		,CT Ct
	){
		var T = Tbl;
		var TW = TblMgr.GetTable<PoWord>();
		var NWordId = nameof(I_WordId.WordId);
		var Sql =
$"""
SELECT * FROM {Tbl.Qt(Tbl.DbTblName)}
WHERE {Tbl.Fld(NWordId)} = {Tbl.Prm(NWordId)}
AND {Tbl.Fld(nameof(IPoBase.Status))} <> {PoStatus.Deleted.Value}
{Tbl.SqlMkr.PrmLmtOfst(out var NLmt, out var NOfst)}
""";
		var SqlCmd = await SqlCmdMkr.Prepare(Ctx, Sql, Ct);
		//var FnCnt = await RepoWord.FnCount(Ctx, Ct);
		var Fn = async(
			IdWord IdWord
			,IPageQuery PageQry
			,CT Ct
		)=>{
			var Arg = new Str_Any{
				[NWordId] = TW.UpperToRaw(IdWord)
				,[NLmt] = PageQry.PageSize
				,[NOfst] = PageQry.Offset_()
			};
			var DbDict = SqlCmd.Args(Arg).Run(Ct);
			u64 Cnt = 0;
			//if(PageQry.HasTotalCount){Cnt = await FnCnt(Ct);}
			IPageAsy<IStr_Any> R = new PageAsy<IStr_Any>{
				PageQry=PageQry,
				TotalCount=Cnt,
				DataAsy=DbDict,
			};
			return R;
		};
		return Fn;

	}

	public async Task<Func<
		IUserCtx
		,IPageQuery
		,CT
		,Task<IPageAsy<PoWord>>
	>> FnPagePoWords(
		IDbFnCtx Ctx
		,CT Ct
	){
		var TW = TblMgr.GetTable<PoWord>();
		var NOwner = nameof(PoWord.Owner);
		var Sql =
$"""
SELECT * FROM {TW.Qt(TW.DbTblName)}
WHERE {TW.Fld(NOwner)} = {TW.Prm(NOwner)}
AND {TW.Fld(nameof(PoWord.Status))} <> {PoStatus.Deleted.Value}
ORDER BY {TW.Fld(nameof(IPoBase.DbCreatedAt))} DESC
{TW.SqlMkr.PrmLmtOfst(out var NLmt, out var NOfst)}
""";
		var SqlCmd = await SqlCmdMkr.Prepare(Ctx, Sql, Ct);
		var FnCnt = await RepoWord.FnCount(Ctx, Ct);
		var Fn = async(
			IUserCtx UserCtx
			,IPageQuery PageQry
			,CT Ct
		)=>{
			var Arg = new Str_Any(){
				[NOwner] = TW.UpperToRaw(UserCtx.UserId)
				,[NLmt] = PageQry.PageSize
				,[NOfst] = PageQry.Offset_()
			};

			var RawDbDicts = SqlCmd.Args(Arg).Run(Ct);
			var PoWords = RawDbDicts.Select(
				(Raw)=>TW.DbDictToPo<PoWord>(Raw)
			);
			var Cnt = PageQry.WantTotalCount?  await FnCnt(Ct)  :  0;
			IPageAsy<PoWord> R = new PageAsy<PoWord>{
				PageQry = PageQry,
				TotalCount = Cnt,
				DataAsy = PoWords
			};
			return R;
		};
		return Fn;
	}

	public async Task<Func<
		IUserCtx
		,IPageQuery
		,CT
		,Task<IPageAsy<JnWord>>
	>> FnPageJnWords(
		IDbFnCtx Ctx
		,CT Ct
	){
		var TK = TblMgr.GetTable<PoWordProp>();
		var TL = TblMgr.GetTable<PoWordLearn>();
		var PagePoWords = await FnPagePoWords(Ctx, Ct);
		var PageKvByFKey = await FnPageByFKey(Ctx, TK, Ct);
		var PageLearnByFKey = await FnPageByFKey(Ctx, TL, Ct);

		var Fn = async(
			IUserCtx UserCtx
			,IPageQuery PageQry
			,CT Ct
		)=>{
			var PoWordsPage = await PagePoWords(UserCtx, PageQry, Ct);
			var R = PageAsy<JnWord>.Mk(PageQry, null, true, PoWordsPage.TotalCount);
			if(PoWordsPage.DataAsy == null){
				return R;
			}

			var JnWords = PoWordsPage.DataAsy.Select(async (PoWord)=>{
				var KvPage = await PageKvByFKey(PoWord.Id, PageQuery.SelectAll(), Ct);
				var Kvs = await _PageAsyToList<PoWordProp>(KvPage, TK);

				var LearnPage = await PageLearnByFKey(PoWord.Id, PageQuery.SelectAll(), Ct);
				var Learns = await _PageAsyToList<PoWordLearn>(LearnPage, TL);

				var R = new JnWord(PoWord, Kvs, Learns);
				return R;
			}).FlattenAsync();
			R.DataAsy = JnWords;
			return R;
		};
		return Fn;
	}

	async Task<IList<TPo>> _PageAsyToList<TPo>(
		IPageAsy<IStr_Any> PageAsy
		,ITable Tbl
	)where TPo:new()
	{
		if(PageAsy.DataAsy == null){
			return new List<TPo>();
		}
		return await PageAsy.DataAsy.Select(
			D=>Tbl.AssignCodePo(D, new TPo())
		).ToListAsync();
	}

/// <summary>
/// 頁查 某時後 改˪ʹ 及 新增ʹ 諸詞 之id
/// </summary>
/// <param name="Ctx"></param>
/// <param name="Ct"></param>
/// <returns></returns>
	public async Task<Func<
		IUserCtx
		,IPageQuery
		,Tempus
		,CT
		,Task<IPageAsy<IdWord>>
	>> FnPage_ChangedWordIdsAfterTime(IDbFnCtx Ctx, CT Ct){
var T = TblMgr.GetTable<PoWord>();
str
	NId = nameof(PoWord.Id),NOwner = nameof(PoWord.Owner)
	,NUpdateAt = nameof(PoWord.UpdatedAt),NCreatedAt = nameof(PoWord.CreatedAt)
;
var PTempus = T.Prm("Tempus");var POwner = T.Prm("Owner");

var Sql =
$"""
SELECT {T.Fld(NId)} AS {T.Qt(NId)}
FROM {T.Qt(T.DbTblName)}
WHERE {T.Fld(NOwner)} = {POwner}
AND (
	{T.Fld(NUpdateAt)} > {PTempus}
	OR {T.Fld(NCreatedAt)} > {PTempus}
)
{T.SqlMkr.PrmLmtOfst(out str Lmt, out str Ofst)}
""";//考慮同步後 一方ʃ新增、此旹無UpdatedAt 只有CreatedAt
var SqlCmd = await SqlCmdMkr.Prepare(Ctx, Sql, Ct);
		var Fn= async(IUserCtx UserCtx, IPageQuery PageQry, Tempus Tem, CT Ct)=>{
			var RawDictAsy = SqlCmd.WithCtx(Ctx).Args(ArgDict.Mk()
				.Add(PTempus, T.UpperToRaw(Tem))
				.Add(POwner, T.UpperToRaw(UserCtx.UserId))
				.AddPageQry(PageQry, Lmt, Ofst)
				.ToDict()
			).Run(Ct);
			var WordIds = RawDictAsy.Select(x => T.RawToUpper<IdWord>(x, NId));
			var R = PageAsy<IdWord>.Mk(PageQry, WordIds);
			R.HasTotalCount = false;
			return R;
		};
		return Fn;
	}



	public async Task<Func<

		Task<nil>
	>> Fn(IDbFnCtx Ctx, CT Ct){

		var Fn = async()=>{
			return NIL;
		};
		return Fn;
	}

#if false
	public async Task<Func<
		Task<nil>
	>> Fn(){
		var Fn = async()=>{
			return NIL;
		};
		return Fn;
	}
#endif
}


// public  partial class Scalar{
// 	public static object Arg = null!;
// }

// public  partial class Expr<T_Po>{
// 	public Expr<T_Po> Select(Expression<Func<T_Po, object>> expr){
// 		return this;
// 	}
// 	public Expr<T_Po> Where(Expression<Func<T_Po, bool>> expr){
// 		return this;
// 	}

// 	public str ToSql(){
// 		return "";
// 	}

// }
