namespace Ngaq.Local.Word.Dao;

using Ngaq.Core.Infra;
using Ngaq.Core.Model.Po;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Model.Po.Word;
using Tsinswreng.CsSqlHelper;
using Str_Any = System.Collections.Generic.Dictionary<str, obj?>;
using IStr_Any = System.Collections.Generic.IDictionary<str, obj?>;
using Ngaq.Core.Infra.Errors;
using Ngaq.Core.Models.Po;
using Tsinswreng.CsPage;
using Tsinswreng.CsTools;
using Ngaq.Local.Db.TswG;
using System.Diagnostics;
using Ngaq.Core.Word.Models.Po.Word;
using System.Linq.Expressions;
using Ngaq.Core.Shared.User.UserCtx;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Shared.Word.Models;
using Ngaq.Core.Shared.Word.Models.Po.Word;
using Ngaq.Core.Shared.Base.Models.Po;
using Ngaq.Core.Shared.Word.Models.Po.Learn;
using Ngaq.Local.Domains.Word.Dao;
using Ngaq.Core.Shared.Word.Models.Dto;
using System.Collections;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.Base.Models.Req;
using Ngaq.Core.Shared.Base.Models.Resp;
using Tsinswreng.CsCore;
using System.Threading.Tasks;
using Acornima.Ast;



public class AutoBatch<TItem, TRet>:BatchCollector<TItem, TRet>{
	public AutoBatch(){

	}
	public static new u64 DfltBatchSize{get;set;} = 100;
	public I_DuplicateSql SqlDuplicator{get;set;}
	//public u64 BatchSize;
	public ISqlCmd FullBatch{get;set;} = null!;
	public ISqlCmd FinalBatch{get;set;} = null!;
	public IDbFnCtx Ctx{get;set;}
	public ISqlCmdMkr SqlCmdMkr{get;set;}
	public ISqlCmd SqlCmd{get;set;}
	public static AutoBatch<TItem, TRet> Mk(
		IDbFnCtx Ctx
		,ISqlCmdMkr SqlCmdMkr
		,I_DuplicateSql SqlDuplicator
		,Func<
			AutoBatch<TItem, TRet> //Self
			,IList<TItem>
			,CT
			,Task<TRet>
		> FnAsy
		,u64 BatchSize = 0
	){
		if(BatchSize == 0){
			BatchSize = DfltBatchSize;
		}
		var R = new AutoBatch<TItem, TRet>();
		var ArgFn = FnAsy;
		R.FnAsy = async(Items, Ct)=>{
			var size = (u64)Items.Count;
			R.SqlCmd = R.FullBatch;
			var FnGetRepeatedSql = R.SqlDuplicator.DuplicateSql;
			if((u64)Items.Count < R.BatchSize){
				R.FinalBatch = await R.Ctx.PrepareToDispose(R.SqlCmdMkr, FnGetRepeatedSql(size), Ct);
				R.SqlCmd = R.FinalBatch;
			}else if(R.FullBatch == null){
				R.FullBatch = await R.Ctx.PrepareToDispose(R.SqlCmdMkr, FnGetRepeatedSql(R.Ctx.BatchSize), Ct);
				R.SqlCmd = R.FullBatch;
			}
			return await ArgFn(R, Items, Ct);
		};
		R.Init(R.FnAsy, BatchSize);
		return R;
	}

}

public partial class DaoWord{

	public async Task<IAsyncEnumerable<IdWord?>> SlctIdByOwnerHeadLangWithDelBatch(
		IDbFnCtx Ctx, IUserCtx User, IEnumerable<Head_Lang> HeadLangs, CT Ct
	){
		var Sql = T.SqlSplicer().Select(x=>x.Id).From().Where1()
		.AndEq(x=>x.Owner, out var POwner)
		.AndEq(x=>x.Head, out var PHead)
		.AndEq(x=>x.Lang, out var PLang)
		;

		await using var batch = AutoBatch<Head_Lang, IAsyncEnumerable<IdWord?>>.Mk(
			Ctx, SqlCmdMkr, Sql,
			async(z, HeadLangs, Ct)=>{
				var Head = HeadLangs.Select(x=>x.Head);
				var Lang = HeadLangs.Select(x=>x.Lang);
				var UserId = User.UserId;
				var Args = ArgDict.Mk(T)
				.AddT(POwner, UserId)
				.AddManyT(PHead, Head)
				.AddManyT(PLang, Lang);
				var GotDicts = z.SqlCmd.Args(Args).AsyE1d(Ct).OrEmpty();
				return GotDicts.Select(x=>{//TODO 當此組 (Head,Lang)查不到數據旹 會返null否
					var ans = x[T.Memb(x=>x.Id)];
					return (IdWord?)IdWord.FromByteArr((u8[])ans!);
				});
			}
		);
		var R = batch.AddToEnd(HeadLangs, Ct);
		return R.Flat();
	}

	public async Task<Func<
		IUserCtx,
		IEnumerable<Head_Lang>,
		CT
		,Task<IAsyncEnumerable<IdWord?>>
	>>
	FnSlctIdByOwnerHeadLangWithDelBatch(IDbFnCtx Ctx,CT Ct){//嘗試批量操作(未完成)

var Sql = T.SqlSplicer().Select(x=>x.Id).From().Where1()
.AndEq(x=>x.Owner, out var POwner)
.AndEq(x=>x.Head, out var PHead)
.AndEq(x=>x.Lang, out var PLang)
// .ToSqlStr(Ctx)//Ctx中有BatchSize
;
		ISqlCmd FullBatch = null!, FinalBatch = null!;
		return async (User,HeadLangs,Ct)=>{
			await using var batch = new BatchCollector<Head_Lang, IAsyncEnumerable<IdWord?>>(async(HeadLangs, Ct)=>{
				var size = (u64)HeadLangs.Count;
				var SqlCmd = FullBatch;
				if((u64)HeadLangs.Count < Ctx.BatchSize){
					FinalBatch = await Ctx.PrepareToDispose(SqlCmdMkr, Sql.ToSqlStr(size), Ct);
					SqlCmd = FinalBatch;
				}else if(FullBatch == null){
					FullBatch = await Ctx.PrepareToDispose(SqlCmdMkr, Sql.ToSqlStr(Ctx.BatchSize), Ct);
					SqlCmd = FullBatch;
				}

				var Head = HeadLangs.Select(x=>x.Head);
				var Lang = HeadLangs.Select(x=>x.Lang);
				var UserId = User.UserId;
				var Args = ArgDict.Mk(T)
				.AddT(POwner, UserId)
				.AddManyT(PHead, Head)
				.AddManyT(PLang, Lang);
				var GotDicts = SqlCmd.Args(Args).AsyE1d(Ct).OrEmpty();
				return GotDicts.Select(x=>{//TODO 當此組 (Head,Lang)查不到數據旹 會返null否
					var ans = x[T.Memb(x=>x.Id)];
					return (IdWord?)IdWord.FromByteArr((u8[])ans!);
				});
			}, Ctx.BatchSize);
			var R = batch.AddToEnd(HeadLangs, Ct);
			return R.Flat();
		};
	}

	public async Task<Func<
		IUserCtx,
		str,//Head
		str,//Lang
		CT
		,Task<IdWord?>
	>>
	FnSlctIdByOwnerHeadLangWithDel(IDbFnCtx Ctx,CT Ct){
var Sql = T.SqlSplicer().Select(x=>x.Id).From().Where1()
.AndEq(x=>x.Owner, out var POwner)
.AndEq(x=>x.Head, out var PHead)
.AndEq(x=>x.Lang, out var PLang)
.ToSqlStr()
;
		var SqlCmd = await Ctx.PrepareToDispose(SqlCmdMkr, Sql, Ct);
		return async (User,Head,Lang,Ct)=>{
			var UserId = User.UserId;
			var Args = ArgDict.Mk(T)
			.AddT(POwner, UserId)
			.AddT(PHead, Head)
			.AddT(PLang, Lang);
			var GotDict = await SqlCmd.Args(Args).AsyE1d(Ct).FirstOrDefaultAsync(Ct);
			if(GotDict == null){
				return null;
			}
			var ans = GotDict[T.Memb(x=>x.Id)];
			return IdWord.FromByteArr((u8[])ans!);
		};
	}


// 	public async Task<Func<
// 		IEnumerable<IdUser>,
// 		IEnumerable<str>,//Head
// 		IEnumerable<str>,//Lang
// 		CT
// 		,Task<IList<IdWord?>>
// 	>>
// 	FnSlctListIdByOwnerHeadLang(IDbFnCtx Ctx,CT Ct){
// 		var T = TblMgr.GetTbl<PoWord>(); var N = new PoWord.N();
// 		var POwner = T.Prm(N.Owner);var PHead = T.Prm(N.Head); var PLang = T.Prm(N.Lang);
// var fnSql = (u64 i)=>{
// return $"""
// SELECT {T.Fld(N.Id)} AS {T.Qt(N.Id)}
// FROM {T.Qt(T.DbTblName)}
// WHERE 1=1
// AND {T.SqlIsNonDel()}
// AND {T.Eq(POwner.Name, POwner[i])}
// AND {T.Eq(PHead.Name, PHead[i])}
// AND {T.Eq(PLang.Name, PLang[i])}
// """;
// };
// 		var SqlCmd = await Ctx.PrepareToDispose(SqlCmdMkr, Sql, Ct);
// 		return async (User,Head,Lang,Ct)=>{
// 			var UserId = User.UserId;
// 			var Args = ArgDict.Mk(T)
// 			.AddT(POwner, UserId)
// 			.AddT(PHead, Head)
// 			.AddT(PLang, Lang);
// 			var GotDict = await SqlCmd.Args(Args).IterAsyE(Ct).FirstOrDefaultAsync(Ct);
// 			if(GotDict == null){
// 				return null;
// 			}
// 			var ans = GotDict[N.Id];
// 			return IdWord.FromByteArr((u8[])ans!);
// 		};
// 	}


	public async Task<Func<
		IdWord
		,CT
		,Task<JnWord?>
	>> FnSlctJnWordByIdWithDel(
		IDbFnCtx Ctx
		,CT Ct
	){
		var PWordId = TP.Prm(nameof(PoWordProp.WordId));
		var Sql_SeekByFKey = (str QuotedTblName)=>{
			var Sql = TW.SqlSplicer().Select("*").From(QuotedTblName)
			.Where1().And(TP.Eq(PWordId)).ToSqlStr();
			return Sql;
		};
		var GetPoWordById = await RepoWord.FnSlctOneById(Ctx, Ct);
		var Cmd_SeekKv = await SqlCmdMkr.Prepare(Ctx, Sql_SeekByFKey(TP.Qt(TP.DbTblName)), Ct);
		var Cmd_SeekLearn = await SqlCmdMkr.Prepare(Ctx, Sql_SeekByFKey(TL.Qt(TL.DbTblName)), Ct);

		return async(Id,Ct)=>{
			var PoWord = await GetPoWordById(Id, Ct);
			if(PoWord == null){
				return null;
			}
			var Arg = new Str_Any{
				[nameof(PoWordProp.WordId)] = Id
			};
			var RawPropDicts = (await Cmd_SeekKv.RawArgs(TP.ToDbDict(Arg)).All1d(Ct))
				.Select(dbDict=>TP.DbDictToEntity<PoWordProp>(dbDict))
				.ToList()
			;
			var RawLearnDicts = (await Cmd_SeekLearn.RawArgs(TL.ToDbDict(Arg)).All1d(Ct))
				.Select(dbDict=>TL.DbDictToEntity<PoWordLearn>(dbDict))
				.ToList()
			;
			var ans = new JnWord{
				Word = PoWord
				,Props = RawPropDicts
				,Learns = RawLearnDicts
			};
			return ans;
		};
	}

	static str SqlFilterDel(
		ITable Tbl,
		bool IncludeDel
	){
		return IncludeDel
			? ""
			: $"AND {Tbl.SqlIsNonDel()}";
	}

	public async Task<Func<
		IdWord
		,IPageQry
		,CT
		,Task<IPageAsyE<IStr_Any>>
	>> FnPageByWordIdOld(
		IDbFnCtx Ctx
		,ITable Tbl
		,OptQry CfgQry
		,CT Ct
	){
		var T = Tbl;
		var TW = TblMgr.GetTbl<PoWord>();
		var PWordId = T.Prm(nameof(I_WordId.WordId));
		var FilterDel = SqlFilterDel(T, CfgQry.IncludeDeleted);
		var Sql =
$"""
SELECT * FROM {Tbl.Qt(Tbl.DbTblName)}
WHERE 1=1
{FilterDel}
AND {T.Eq(PWordId)}
{Tbl.SqlMkr.ParamLimOfst(out var PLmt, out var POfst)}
""";
		var SqlCmd = await Ctx.PrepareToDispose(SqlCmdMkr, Sql, Ct);
		return async(IdWord, PageQry ,Ct)=>{
			var Arg = ArgDict.Mk(T)
			.AddT(PWordId, IdWord)
			.AddPageQry(PageQry, PLmt, POfst);
			var DbDict = Ctx.RunCmd(SqlCmd, Arg).AsyE1d(Ct);
			u64 Cnt = 0;
			//if(PageQry.HasTotalCount){Cnt = await FnCnt(Ct);}
			IPageAsyE<IStr_Any> R = new PageAsyE<IStr_Any>{
				PageQry=PageQry,
				TotCnt=Cnt,
				DataAsyE=DbDict,
			};
			return R;
		};
	}


	public async Task<Func<
		IList<IdWord>
		,IPageQry
		,CT
		,Task<IPageAsyE<IStr_Any>>
	>> FnPageByWordId(
		IDbFnCtx Ctx
		,ITable Tbl
		,OptQry OptQry
		,CT Ct
	){
		var T = Tbl;
		var TW = TblMgr.GetTbl<PoWord>();
		var PWordId = T.Prm(nameof(I_WordId.WordId));
		var FilterDel = SqlFilterDel(T, OptQry.IncludeDeleted);
		var numParams = T.NumParamsEndStart(OptQry.InParamCnt);
		var Sql =
$"""
SELECT * FROM {Tbl.Qt(Tbl.DbTblName)}
WHERE 1=1
{FilterDel}
AND {T.Fld(PWordId)} IN ({str.Join(",", numParams)})
{Tbl.SqlMkr.ParamLimOfst(out var PLmt, out var POfst)}
""";
		var SqlCmd = await Ctx.PrepareToDispose(SqlCmdMkr, Sql, Ct);
		return async(IdWords, PageQry ,Ct)=>{
			var Arg = ArgDict.Mk(T)
			.AddManyT(numParams, IdWords)
			.AddPageQry(PageQry, PLmt, POfst);
			var DbDict = Ctx.RunCmd(SqlCmd, Arg).AsyE1d(Ct);
			u64 Cnt = 0;
			//if(PageQry.HasTotalCount){Cnt = await FnCnt(Ct);}
			IPageAsyE<IStr_Any> R = new PageAsyE<IStr_Any>{
				PageQry=PageQry,
				TotCnt=Cnt,
				DataAsyE=DbDict,
			};
			return R;
		};
	}


	public async Task<Func<
		IList<IdWord>
		,CT
		,Task<IAsyncEnumerable<IStr_Any>>
	>> FnScltAllByWordIds(
		IDbFnCtx Ctx
		,ITable Tbl
		,OptQry OptQry
		,CT Ct
	){

		var T = Tbl;
		var TW = TblMgr.GetTbl<PoWord>();
		var PWordId = T.Prm(nameof(I_WordId.WordId));
		var FilterDel = SqlFilterDel(T, OptQry.IncludeDeleted);
		var numParams = T.NumParamsEndStart(OptQry.InParamCnt);
		var Sql =
$"""
SELECT * FROM {Tbl.Qt(Tbl.DbTblName)}
WHERE 1=1
{FilterDel}
AND {T.Fld(PWordId)} IN ({str.Join(",", numParams)})
""";
		var SqlCmd = await Ctx.PrepareToDispose(SqlCmdMkr, Sql, Ct);
		return async(IdWords ,Ct)=>{
			var Arg = ArgDict.Mk(T)
			.AddManyT(numParams, IdWords);
			var DbDict = Ctx.RunCmd(SqlCmd, Arg).AsyE1d(Ct);
			return DbDict;
		};
	}




	public async Task<Func<
		IUserCtx
		,IPageQry
		,CT
		,Task<IPageAsyE<PoWord>>
	>> FnPagePoWords(
		IDbFnCtx Ctx
		,OptQry CfgQry
		,CT Ct
	){
		var T = TblMgr.GetTbl<PoWord>();
		var N = new PoWord.N();
		//var POwner = T.Prm(N.Owner);
var Sql = T.SqlSplicer().Select("*").From()
	.Where1()
	.Raw(SqlFilterDel(T, CfgQry.IncludeDeleted))
	.And(x=>x.Owner, "=", out var POwner)
	.OrderByDesc(x=>x.BizCreatedAt)
	.LimOfst(out var PLim, out var POfst)
	.ToSqlStr()
;

		var SqlCmd = await Ctx.PrepareToDispose(SqlCmdMkr, Sql, Ct);
		var FnCnt = await RepoWord.FnCount(Ctx, Ct);
		return async(UserCtx, PageQry ,Ct)=>{
			var Arg = ArgDict.Mk(T)
			.AddT(POwner, UserCtx.UserId)
			.AddPageQry(PageQry, PLim, POfst);

			var RawDbDicts = SqlCmd.Args(Arg).AsyE1d(Ct);
			var PoWords = RawDbDicts.Select(
				(Raw)=>T.DbDictToEntity<PoWord>(Raw)
			);
			var Cnt = PageQry.WantTotCnt?  await FnCnt(Ct)  :  0;
			IPageAsyE<PoWord> R = new PageAsyE<PoWord>{
				PageQry = PageQry,
				TotCnt = Cnt,
				DataAsyE = PoWords
			};
			return R;
		};
	}


	public async Task<Func<
		IUserCtx
		,IPageQry
		,CT
		,Task<IPageAsyE<IJnWord>>
	>> FnPageWordsOld(
		IDbFnCtx Ctx
		,OptQry CfgQry
		,CT Ct
	){
		var TK = TblMgr.GetTbl<PoWordProp>();
		var TL = TblMgr.GetTbl<PoWordLearn>();
		var PagePoWords = await FnPagePoWords(Ctx, CfgQry, Ct);
		var PageKvByFKey = await FnPageByWordIdOld(Ctx, TK, CfgQry, Ct);
		var PageLearnByFKey = await FnPageByWordIdOld(Ctx, TL, CfgQry, Ct);

		return async(UserCtx, PageQry, Ct)=>{
			var PoWordsPage = await PagePoWords(UserCtx, PageQry, Ct);
			var R = PageAsyE<IJnWord>.Mk(PageQry, null, true, PoWordsPage.TotCnt);
			if(PoWordsPage.DataAsyE == null){
				return R;
			}
			var syncPoWordsPage = await PoWordsPage.ToListPage(Ct);//先全部載入內存、否則pg報錯不支持併發查

			async IAsyncEnumerable<IJnWord> fn(
				IEnumerable<PoWord> poWords
			){
				foreach(var PoWord in poWords){
					var KvPage = await PageKvByFKey(PoWord.Id, TK.PageSlctAll(), Ct);
					var syncKvPage = await KvPage.ToListPage(Ct);
					var Kvs = await _PageToList<PoWordProp>(syncKvPage, TK);

					var LearnPage = await PageLearnByFKey(PoWord.Id, TL.PageSlctAll(), Ct);
					var syncLearnPage = await LearnPage.ToListPage(Ct);
					var Learns = await _PageToList<PoWordLearn>(syncLearnPage, TL);

					var ua = new JnWord(PoWord, Kvs, Learns);
					yield return ua;
				}
			}

			R.DataAsyE = fn(syncPoWordsPage.Data??[]);


#if false // Sqlite支持 但pg不支持在同一个连接上并发执行命令。
			var JnWordsTasks = PoWordsPage.Data.Select(async (PoWord)=>{
				try{
					var KvPage = await PageKvByFKey(PoWord.Id, TK.PageSlctAll(), Ct);
					var Kvs = await _PageToList<PoWordProp>(KvPage, TK);

					var LearnPage = await PageLearnByFKey(PoWord.Id, TL.PageSlctAll(), Ct);
					var Learns = await _PageToList<PoWordLearn>(LearnPage, TL);

					var R = new JnWord(PoWord, Kvs, Learns);
					return R;
				}
				catch (System.Exception e){
					System.Console.Error.WriteLine(e);//t
					throw;
				}
			});
			R.Data = await Task.WhenAll(JnWordsTasks);
#endif
			return R;
		};
	}


	public async Task<Func<
		IUserCtx
		,IPageQry
		,CT
		,Task<IPageAsyE<IJnWord>>
	>> FnPageWords(
		IDbFnCtx Ctx
		,OptQry OptQry
		,CT Ct
	){
		var PagePoWords = await FnPagePoWords(Ctx, OptQry, Ct);
		return async(UserCtx, PageQry, Ct)=>{
			var PoWordsPage = await PagePoWords(UserCtx, PageQry, Ct);
			var R = PageAsyE<IJnWord>.Mk(PageQry, null, true, PoWordsPage.TotCnt);
			if(PoWordsPage.DataAsyE == null){
				return R;
			}
			var syncPoWordsPage = await PoWordsPage.ToListPage(Ct);//先全部載入內存、否則pg報錯不支持併發查
			await using var batch = new BatchCollector<PoWord, IList<IJnWord>>(async(PoWords, Ct)=>{
				var Ids = PoWords.Select(x=>x.Id).ToList();
				var NWordId = nameof(I_WordId.WordId);
				var optQry2 = OptQry with { InParamCnt = (u64)Ids.Count };

				var propsById = await RepoWord.IncludeEntitysByKeys(Ctx, NWordId, optQry2, Ids, x=>x.WordId , TP, Ct);
				var learnsById = await RepoWord.IncludeEntitysByKeys(Ctx, NWordId, optQry2, Ids, x=>x.WordId , TL, Ct);

				var result = new List<IJnWord>();
				foreach(var poWord in PoWords){
					var p = propsById.GetValueOrDefault(poWord.Id, []);
					var l = learnsById.GetValueOrDefault(poWord.Id, []);
					result.Add(new JnWord(poWord, p, l));
				}
				return result;
			});
			var poWords = syncPoWordsPage.Data??[];
			var s2d =  batch.AddToEnd(poWords, Ct);
			R.DataAsyE = s2d.Flat();
			return R;
		};
	}


/// <summary>
/// 頁查 某時後 改˪ʹ 及 新增ʹ 諸詞 之id
/// </summary>
/// <param name="Ctx"></param>
/// <param name="Ct"></param>
/// <returns></returns>
	public async Task<Func<
		IUserCtx
		,IPageQry
		,Tempus
		,CT
		,Task<IPageAsyE<IdWord>>
	>> FnPageChangedWordIdsWithDelWordsAfterTime(IDbFnCtx Ctx, CT Ct){
var T = TblMgr.GetTbl<PoWord>();

IParam PTempus=null!;
var Sql = T.SqlSplicer()
.Select(x=>x.Id).From().Where1()
.And(x=>x.Owner, "=", out var POwner)
.And().Paren(b=>
	b.Bool(x=>x.BizUpdatedAt, ">", out var PTempus)
	.Or().Bool(x=>x.StoredAt, ">", PTempus)
).LimOfst(out var Lmt, out var Ofst).ToSqlStr();


str NId = nameof(PoWord.Id);

//str NId = nameof(PoWord.Id)	,NUpdateAt = nameof(PoWord.BizUpdatedAt),NStoredAt = nameof(PoWord.StoredAt);
//var PTempus = T.Prm("Tempus");var POwner = T.Prm("Owner");
// var Sql =
// $"""
// SELECT {T.Fld(NId)} AS {T.Qt(NId)}
// FROM {T.Qt(T.DbTblName)}
// WHERE 1=1
// AND {T.Eq(POwner)}
// AND (
// 	{T.Fld(NUpdateAt)} > {PTempus}
// 	OR {T.Fld(NStoredAt)} > {PTempus}
// )
// {T.SqlMkr.ParamLimOfst(out var Lmt, out var Ofst)}
// """;//考慮同步後 一方ʃ新增、此旹無UpdatedAt 只有CreatedAt
		var SqlCmd = await Ctx.PrepareToDispose(SqlCmdMkr, Sql, Ct);
		return async(UserCtx, PageQry, Tempus, Ct)=>{
			var RawDictAsy = SqlCmd.AttachCtxTxn(Ctx).Args(ArgDict.Mk(T)
				.AddT(PTempus, Tempus)
				.AddT(POwner, UserCtx.UserId)
				.AddPageQry(PageQry, Lmt, Ofst)
			).AsyE1d(Ct);
			var WordIds = RawDictAsy.Select(x => T.RawToUpper<IdWord>(x[NId], NId));
			var R = PageAsyE<IdWord>.Mk(PageQry, WordIds);
			R.HasTotCnt = false;
			return R;
		};
	}

	public async Task<Func<
		IUserCtx
		,IPageQry
		,ReqSearchWord
		,CT
		,Task<IPageAsyE<IdWord>>
	>> FnPageSearchWordIdsByHeadPrefixWithDel(IDbFnCtx Ctx, CT Ct){
var T = TblMgr.GetTbl<PoWord>();
var N = new PoWord.N();
var PPrefix = T.Prm("Prefix"); var POwner = T.Prm(N.Owner); var PLang = T.Prm(N.Lang);
/*
T.Select(x=>x.Id)
.And(x=>x.Head, $"LIKE {PPrefix} || '%' ")
.And(x=>x.Owner, $"={POwner}")
//also: //.And(x=>x.Owner, "=", out var POwner)
.OrderBy(x=>x.Head)
.LimOfst(out var PLim, out var POfst2)
 */

;
var Sql =
$"""
SELECT {N.Id} FROM {T.DbTblName}
WHERE 1=1
AND {T.Fld(x=>x.Head)} LIKE {PPrefix} || '%'
AND {T.Eq(POwner)}
ORDER BY {T.Fld(N.Head)} ASC
{T.SqlMkr.ParamLimOfst(out var PLmt, out var POfst)}
"""; // AND {T.Fld(NLang)} = {PLang}
		var SqlCmd = await SqlCmdMkr.Prepare(Ctx, Sql, Ct);
		Ctx?.AddToDispose(SqlCmd);
		return async(User, PageQry, Req, Ct)=>{
			var Arg = ArgDict.Mk(T)
			.AddRaw(PPrefix, Req.RawStr)
			.AddRaw(PLang, Req.Lang)
			.AddT(POwner, User.UserId)
			.AddPageQry(PageQry, PLmt, POfst)
			;
			var RawDicts = SqlCmd.AttachCtxTxn(Ctx).Args(Arg).AsyE1d(Ct);
			var WordIds = RawDicts.Select(d=>{
				var Id = d[N.Id];
				return T.RawToUpper<IdWord>(Id, N.Id);
			});
			var R = PageAsyE<IdWord>.Mk(PageQry, WordIds);
			R.HasTotCnt = false;
			return R;
		};
	}

	public async Task<Func<
		obj
		,CT
		,Task<IdWord?>
	>> FnSlctRootIdByUpperId(IDbFnCtx? Ctx, ITable Tbl, CT Ct){
var NWordId = nameof(I_WordId.WordId); var NId = nameof(I_Id<nil>.Id);
var PId = Tbl.Prm(NId);

var Sql =
$"""
SELECT {Tbl.Fld(NWordId)} AS {Tbl.Qt(NWordId)} FROM {Tbl.DbTblName}
WHERE 1=1
AND {Tbl.Eq(PId)}
""";
		var SqlCmd = await SqlCmdMkr.Prepare(Ctx, Sql, Ct);
		Ctx?.AddToDispose(SqlCmd);
		return async (Id, Ct)=>{
			var Arg = ArgDict.Mk()
			.AddRaw(PId, Tbl.UpperToRaw(Id, NId));
			var RawDicts = await SqlCmd.AttachCtxTxn(Ctx).Args(Arg).All1d(Ct);
			if(RawDicts.Count == 0){
				return null;
			}
			var RawWordId = RawDicts.First()[NWordId];
			return Tbl.RawToUpper<IdWord>(RawWordId, NWordId);
		};
	}

	public async Task<Func<
		IdWordProp
		,CT
		,Task<IdWord?>
	>> FnSlctRootIdByPropId(IDbFnCtx? Ctx, CT Ct){
		var Fn = await FnSlctRootIdByUpperId(Ctx, TblMgr.GetTbl<PoWordProp>(), Ct);
		return async(PropId, Ct)=>{
			return await Fn(PropId, Ct);
		};
	}

	public async Task<Func<
		IdWordLearn
		,CT
		,Task<IdWord?>
	>> FnSlctRootIdByLearnId(IDbFnCtx? Ctx, CT Ct){
		var Fn = await FnSlctRootIdByUpperId(Ctx, TblMgr.GetTbl<PoWordLearn>(), Ct);
		return async(LearnId, Ct)=>{
			return await Fn(LearnId, Ct);
		};
	}

	public async Task<Func<
		CT, Task<nil>
	>> FnTextMultiSelect(IDbFnCtx Ctx, CT Ct){
		var sqls = new List<str>();
for(var i = 0; i < 500; i++){
	sqls.Add($"""
SELECT "Head", "Lang" FROM "Word" WHERE "Head" = '{i}';
""");
}
var Sql = str.Join("\n", sqls);
var Cmd = await Ctx.PrepareToDispose(SqlCmdMkr, Sql, Ct);
		return async(Ct)=>{
			var sw = Stopwatch.StartNew();
			var R2d = await Cmd.All2d(Ct);
			sw.Stop();
			if(R2d is null){return NIL;}
			foreach(var R1d in R2d){
				foreach(var Dict in R1d){
					System.Console.WriteLine(
						str.Join(",", Dict.Values)
					);
				}
			}
			System.Console.WriteLine($"耗时 {sw.ElapsedMilliseconds} ms");
			System.Console.WriteLine(R2d.Count);
			return NIL;
		};
	}




	public async Task<Func<
		ReqScltWordsOfLearnResultByTimeInterval
		,CT, Task<RespScltWordsOfLearnResultByTimeInterval>
	>> FnScltAddedWordsByTimeInterval(IDbFnCtx Ctx, CT Ct){
var T = TblMgr.GetTbl<PoWordLearn>();
var NWordId = nameof(PoWordLearn.WordId);var NLearnResult = nameof(PoWordLearn.LearnResult); var NBizCreatedAt = nameof(PoWordLearn.BizCreatedAt);
var NStartTime = "StartTime"; var NEndTime = "EndTime"; var NNewWords = "NewWords";
var PTimeStart = T.Prm("TimeStart");var PTimeEnd = T.Prm("TimeEnd"); var PTimeInterval = T.Prm("TimeInterval");
var PLearnResult = T.Prm("LearnResult");

var Sql = $"""
WITH RECURSIVE ts_range AS (
	-- 1. 先把时间轴切出来
	SELECT
		{PTimeStart} AS start_ts,
		{PTimeStart} + {PTimeInterval} AS end_ts
	UNION ALL
	SELECT
		end_ts,
		end_ts + {PTimeInterval}
	FROM ts_range
	WHERE end_ts < {PTimeEnd}
),
first_ AS (
	-- 2. 每个单词第一次被 $LearnResult 的时间
	SELECT
		WordId,
		MIN({T.Fld(NBizCreatedAt)}) AS first_ts
	FROM {T.Qt(T.DbTblName)}
	WHERE {T.Fld(NLearnResult)} = {PLearnResult}
	AND {T.Fld(NBizCreatedAt)} >= {PTimeStart}          -- 只关心 tFrom 之后
	GROUP BY {T.Fld(NWordId)}
)
-- 3. 把单词落到对应区间里计数
SELECT
	r.start_ts as {T.Qt(NStartTime)},
	r.end_ts as {T.Qt(NEndTime)},
	COUNT(f.{T.Fld(NWordId)}) AS {T.Qt(NNewWords)}
FROM ts_range r
LEFT JOIN first_ f
	ON f.first_ts >= r.start_ts
	AND f.first_ts <  r.end_ts
GROUP BY r.start_ts, r.end_ts
ORDER BY r.start_ts --DESC
{T.SqlMkr.ParamLimOfst(out var Lmt, out var Ofst)}
;
""";
var Cmd = await Ctx.PrepareToDispose(SqlCmdMkr, Sql, Ct);
		return async(Req, Ct)=>{
			var Arg = ArgDict.Mk(T)
			.AddT(PTimeStart, Req.TimeStart)
			.AddT(PTimeEnd, Req.TimeEnd)
			.AddT(PTimeInterval, Req.TimeInterval)
			.AddT(PLearnResult, Req.LearnResult)
			.AddPageQry(Req.PageQry, Lmt, Ofst);
			var RawDicts = Cmd.AttachCtxTxn(Ctx).Args(Arg).AsyE1d(Ct);
			var Intervals = RawDicts.Select(x=>{
				return new TimeIntervalCnt{
					TimeStart = new Tempus(Convert.ToInt64(x[NStartTime])),
					TimeEnd = new Tempus(Convert.ToInt64(x[NEndTime])),
					Cnt = Convert.ToInt64(x[NNewWords])
				};
			});
			//Req.PageQry,
			var RPage = PageAsyE<TimeIntervalCnt>.Mk(Req.PageQry, Intervals, false);
			var R = new RespScltWordsOfLearnResultByTimeInterval{
				IntervalPage = RPage
			};
			return R;
		};
	}

}
