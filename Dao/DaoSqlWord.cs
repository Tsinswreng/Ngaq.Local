using System.Linq.Expressions;
using Ngaq.Core.Infra;
using Ngaq.Core.Model.Bo;
using Ngaq.Core.Model.Po;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Model.UserCtx;
using Ngaq.Core.Tools;
using Ngaq.Local.Db;
using Tsinswreng.SqlHelper;
using Tsinswreng.SqlHelper.Cmd;
using Tsinswreng.CsCore.Tools;
using Str_Any = System.Collections.Generic.Dictionary<string, object?>;
using IStr_Any = System.Collections.Generic.IDictionary<string, object?>;
using Ngaq.Core.Infra.Page;
using System.Threading.Tasks;
namespace Ngaq.Local.Dao;


public class DaoSqlWord(
	ISqlCmdMkr SqlCmdMkr
	,ITableMgr TblMgr
	,RepoSql<PoWord, IdWord> RepoWord
	,RepoSql<PoKv, IdKv> RepoKv
	,RepoSql<PoLearn, IdLearn> RepoLearn
){
	// public RepoSql<Po_Word, IdWord> RepoWord{get;set;}
	// public RepoSql<Po_Kv, IdKv> RepoKv{get;set;}
	// public RepoSql<Po_Learn, IdKv> RepoLearn{get;set;}

	// public ISqlCmdMkr SqlCmdMkr{get;set;}
	// public ITableMgr TblMgr{get;set;}


	// public async Task<Func<
	// 	IUserCtx
	// 	,CancellationToken
	// 	,Task<IEnumerable<BoWord>>
	// >> FnSelectAllWords(
	// 	IDbFnCtx Ctx
	// 	,CancellationToken Ct
	// ){

	// 	var Fn = async(
	// 		IUserCtx UserCtx
	// 		,CancellationToken Ct
	// 	)=>{

	// 	};
	// 	return Fn;
	// }


	public async Task<Func<
		IUserCtx
		,str
		,str
		,CancellationToken
		,Task<IdWord?>
	>>
	FnSelectIdByHeadEtLang(
		IDbFnCtx Ctx
		,CancellationToken ct
	){
		var T = TblMgr.GetTable<PoWord>();
		var F = TblMgr.SqlMkr;
		var Sql =
$"""
SELECT {T.Field(nameof(I_Id<nil>.Id))} FROM {T.Quote(T.Name)}
WHERE {T.Field(nameof(PoWord.Owner))} = {F.Param(nameof(PoWord.Owner))}
AND {T.Field(nameof(PoWord.Head))} = {F.Param(nameof(PoWord.Head))}
AND {T.Field(nameof(PoWord.Lang))} = {F.Param(nameof(PoWord.Lang))}
""";
		var SqlCmd = await SqlCmdMkr.Prepare(Ctx, Sql, ct);

/*
SELECT Id FROM Word
WHERE Owner = @Owner
AND WordFormId = @WordFormId
AND Lang = @Lang
 */

		return async (
			IUserCtx OperatorCtx
			,str Head
			,str Lang
			,CancellationToken ct
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
			return new IdWord(ToolId.ByteArrToUInt128((u8[])ans));
		};
	}


/// <summary>
///
/// </summary>
/// <param name="ct"></param>
/// <returns></returns>
	public async Task<Func<
		IdWord
		,CancellationToken
		,Task<JnWord?>
	>> FnSelectJnWordById(
		IDbFnCtx? Ctx
		,CancellationToken ct
	){
		var TW = TblMgr.GetTable<PoWord>();
		var TK = TblMgr.GetTable<PoKv>();
		var TL = TblMgr.GetTable<PoLearn>();
		var Sql_SeekByFKey = (str QuotedTblName)=>{
			var Sql =
$"""
SELECT * FROM {QuotedTblName}
WHERE {TK.Field(nameof(PoKv.FKeyUInt128))} = {TW.Param(nameof(PoKv.FKeyUInt128))}
""";
			return Sql;
		};
		var GetPoWordById = await RepoWord.FnSelectById<IdWord>(Ctx, ct);
		var Cmd_SeekKv = await SqlCmdMkr.Prepare(Ctx, Sql_SeekByFKey(TK.Quote(TK.Name)), ct);
		var Cmd_SeekLearn = await SqlCmdMkr.Prepare(Ctx, Sql_SeekByFKey(TL.Quote(TL.Name)), ct);

		var Fn = async(
			IdWord Id
			,CancellationToken ct
		)=>{
			var Po_Word = await GetPoWordById(Id, ct);
			if(Po_Word == null){
				return null;
			}
			var Arg = new Str_Any{
				[nameof(PoKv.FKeyUInt128)] = Id.Value
			};
			var RawPropDicts = await Cmd_SeekKv.Args(TK.ToDbDict(Arg)).Run(ct)
				.Select(dbDict=>TK.DbDictToPo<PoKv>(dbDict))
				.ToListAsync(ct)
			;
			var RawLearnDicts = await Cmd_SeekLearn.Args(TL.ToDbDict(Arg)).Run(ct)
				.Select(dbDict=>TL.DbDictToPo<PoLearn>(dbDict))
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
		,CancellationToken
		,Task<nil>
	>> FnInsertBoWords(
		IDbFnCtx? Ctx
		,CancellationToken ct
	) {
		var InsertPoWords = await RepoWord.FnInsertMany(Ctx, ct);
		var InsertPoKvs = await RepoKv.FnInsertMany(Ctx, ct);
		var InsertPoLearns = await RepoLearn.FnInsertMany(Ctx, ct);

		var Fn = async(
			IEnumerable<JnWord> BoWords
			,CancellationToken ct
		)=>{
			u64 BatchSize = 0xfff;
			using var Po_Words = new BatchListAsy<PoWord, nil>(async(list, ct)=>{
				await InsertPoWords(list,ct);
				return Nil;
			}, BatchSize);
			using var Po_Kvs = new BatchListAsy<PoKv, nil>(async(e, ct)=>{
				await InsertPoKvs(e, ct);
				return Nil;
			}, BatchSize);
			using var Po_Learns = new BatchListAsy<PoLearn, nil>(async(e, ct)=>{
				await InsertPoLearns(e, ct);
				return Nil;
			}, BatchSize);
			u64 i = 0;
			foreach (var BoWord in BoWords) {
				BoWord.AssignId();
				await Po_Words.Add(BoWord.PoWord, ct);
				foreach (var Prop in BoWord.Props) {
					await Po_Kvs.Add(Prop, ct);
				}
				foreach (var Learn in BoWord.Learns) {
					await Po_Learns.Add(Learn, ct);
				}
				i++;
			}
			await Po_Words.End(ct);
			await Po_Kvs.End(ct);
			await Po_Learns.End(ct);
			return Nil;
		};
		return Fn;
	}




	public async Task<Func<
		IEnumerable<PoKv>
		,CancellationToken
		,Task<nil>
	>> FnInsertPoKvs(
		IDbFnCtx? Ctx
		,CancellationToken ct
	){
		var InsertMany = await RepoKv.FnInsertMany(Ctx, ct);
		var Fn = async(
			IEnumerable<PoKv> Po_Kvs
			,CancellationToken ct
		)=>{
			await InsertMany(Po_Kvs, ct);
			return Nil;
		};
		return Fn;
	}

	public async Task<Func<
		IEnumerable<PoLearn>
		,CancellationToken
		,Task<nil>
	>> FnInsertPoLearns(
		IDbFnCtx? Ctx
		,CancellationToken ct
	){
		var InsertMany = await RepoLearn.FnInsertMany(Ctx, ct);
		var Fn = async(
			IEnumerable<PoLearn> PoLearns
			,CancellationToken ct
		)=>{
			await InsertMany(PoLearns, ct);
			return Nil;
		};
		return Fn;
	}

	public async Task<Func<
		IdWord
		,IPageQuery
		,CancellationToken
		,Task<IPageAsy<IStr_Any>>
	>> FnPageByFKey(
		IDbFnCtx Ctx
		,ITable Tbl
		,CancellationToken Ct
	){
		var T = Tbl;
		var TW = TblMgr.GetTable<PoWord>();
		var NFKey = nameof(IPoKv.FKeyUInt128);
		str NLmt = "Lmt", NOfst = "Ofst";
		var Sql =
$"""
SELECT * FROM {Tbl.Quote(Tbl.Name)}
WHERE {Tbl.Field(NFKey)} = {Tbl.Param(NFKey)}
{Tbl.SqlMkr.LimitOffset(NLmt, NOfst)}
""";
		var SqlCmd = await SqlCmdMkr.Prepare(Ctx, Sql, Ct);
		//var FnCnt = await RepoWord.FnCount(Ctx, Ct);
		var Fn = async(
			IdWord IdWord
			,IPageQuery PageQry
			,CancellationToken Ct
		)=>{
			var Arg = new Str_Any{
				[NFKey] = TW.ToDbType(nameof(PoWord.Id), IdWord)
				,[NLmt] = PageQry.PageSize
				,[NOfst] = PageQry.Offset()
			};
			var DbDict = SqlCmd.Args(Arg).Run(Ct);
			u64 Cnt = 0;
			//if(PageQry.HasTotalCount){Cnt = await FnCnt(Ct);}
			var R = PageAsy<IStr_Any>.Mk(
				PageQry, Cnt, DbDict
			);
			return R;
		};
		return Fn;

	}

	public async Task<Func<
		IUserCtx
		,IPageQuery
		,CancellationToken
		,Task<IPageAsy<PoWord>>
	>> FnPagePoWords(
		IDbFnCtx Ctx
		,CancellationToken Ct
	){
		var TW = TblMgr.GetTable<PoWord>();
		var NOwner = nameof(PoWord.Owner);
		str NLmt = "Lmt", NOfst = "Ofst";
		var Sql =
$"""
SELECT * FROM {TW.Quote(TW.Name)}
WHERE {TW.Field(NOwner)} = {TW.Param(NOwner)}
ORDER BY {TW.Field(nameof(IPoBase.CreatedAt))} DESC
{TW.SqlMkr.LimitOffset(NLmt, NOfst)}
""";
		var SqlCmd = await SqlCmdMkr.Prepare(Ctx, Sql, Ct);
		var FnCnt = await RepoWord.FnCount(Ctx, Ct);
		var Fn = async(
			IUserCtx UserCtx
			,IPageQuery PageQry
			,CancellationToken Ct
		)=>{
			var Arg = new Str_Any(){
				[NOwner] = TW.ToDbType(NOwner, UserCtx.UserId)
				,[NLmt] = PageQry.PageSize
				,[NOfst] = PageQry.Offset()
			};

			var RawDbDicts = SqlCmd.Args(Arg).Run(Ct);
			var PoWords = RawDbDicts.Convert(
				(Raw)=>TW.DbDictToPo<PoWord>(Raw)
			);
			var Cnt = PageQry.HasTotalCount?  await FnCnt(Ct)  :  0;
			var R = PageAsy<PoWord>.Mk(
				PageQry, Cnt, PoWords
			);
			return R;
		};
		return Fn;
	}

	public async Task<Func<
		IUserCtx
		,IPageQuery
		,CancellationToken
		,Task<IPageAsy<JnWord>>
	>> FnPageBoWords(
		IDbFnCtx Ctx
		,CancellationToken Ct
	){
		var TK = TblMgr.GetTable<PoKv>();
		var TL = TblMgr.GetTable<PoLearn>();
		var PagePoWords = await FnPagePoWords(Ctx, Ct);
		var PageKvByFKey = await FnPageByFKey(Ctx, TK, Ct);
		var PageLearnByFKey = await FnPageByFKey(Ctx, TL, Ct);

		var Fn = async(
			IUserCtx UserCtx
			,IPageQuery PageQry
			,CancellationToken Ct
		)=>{
			var PoWordsPage = await PagePoWords(UserCtx, PageQry, Ct);
			var R = PageAsy<JnWord>.Mk(PageQry, PoWordsPage.TotalCount, null);
			if(PoWordsPage.DataAsy == null){
				return R;
			}

			var BoWords = PoWordsPage.DataAsy.Convert(async (PoWord)=>{
				var KvPage = await PageKvByFKey(PoWord.Id, PageQuery.SelectAll(), Ct);
				var Kvs = await _PageAsyToList<PoKv>(KvPage, TK);

				var LearnPage = await PageLearnByFKey(PoWord.Id, PageQuery.SelectAll(), Ct);
				var Learns = await _PageAsyToList<PoLearn>(LearnPage, TL);

				var R = new JnWord(PoWord, Kvs, Learns);
				return R;
			}).FlattenAsync();
			R.DataAsy = BoWords;
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
		return await PageAsy.DataAsy.Convert(D=>Tbl.AssignCodePo(D, new TPo())).ToListAsync();
	}
}


// public class Scalar{
// 	public static object Arg = null!;
// }

// public class Expr<T_Po>{
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



// /*

//  */

