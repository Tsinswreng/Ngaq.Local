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

public partial class DaoSqlWord{
	public async Task<Func<
		IUserCtx,
		str,//Head
		str,//Lang
		CT
		,Task<IdWord?>
	>>
	FnSlctIdByOwnerHeadLang(IDbFnCtx Ctx,CT Ct){
		var T = TblMgr.GetTbl<PoWord>(); var N = new PoWord.N();
		var POwner = T.Prm(N.Owner);var PHead = T.Prm(N.Head); var PLang = T.Prm(N.Lang);
		var Sql =
$"""
SELECT {T.Fld(N.Id)} AS {T.Qt(N.Id)}
FROM {T.Qt(T.DbTblName)}
WHERE 1=1
AND {T.SqlIsNonDel()}
AND {T.Eq(POwner)}
AND {T.Eq(PHead)}
AND {T.Eq(PLang)}
""";
		var SqlCmd = await SqlCmdMkr.Prepare(Ctx, Sql, Ct);
		return async (User,Head,Lang,Ct)=>{
			var UserId = User.UserId;
			var Args = ArgDict.Mk(T)
			.AddT(POwner, UserId)
			.AddT(PHead, Head)
			.AddT(PLang, Lang);
			var GotDict = await SqlCmd.Args(Args).IterAsyE(Ct).FirstOrDefaultAsync(Ct);
			if(GotDict == null){
				return null;
			}
			var ans = GotDict[N.Id];
			return IdWord.FromByteArr((u8[])ans!);
		};
	}

	public async Task<Func<
		IdWord
		,CT
		,Task<JnWord?>
	>> FnSlctJnWordByIdWithDel(
		IDbFnCtx? Ctx
		,CT Ct
	){
		var TW = TblMgr.GetTbl<PoWord>();
		var TP = TblMgr.GetTbl<PoWordProp>();
		var TL = TblMgr.GetTbl<PoWordLearn>();
		var PWordId = TP.Prm(nameof(PoWordProp.WordId));
		var Sql_SeekByFKey = (str QuotedTblName)=>{
			var Sql =
$"""
SELECT * FROM {QuotedTblName}
WHERE 1=1
AND {TP.Eq(PWordId)}
""";
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
			// if(PoWord.IsDeleted()){
			// 	return null;
			// }

			var Arg = new Str_Any{
				[nameof(PoWordProp.WordId)] = Id
			};
			var RawPropDicts = (await Cmd_SeekKv.RawArgs(TP.ToDbDict(Arg)).All(Ct))
				.Select(dbDict=>TP.DbDictToEntity<PoWordProp>(dbDict))
				.ToList()
			;

			var RawLearnDicts = (await Cmd_SeekLearn.RawArgs(TL.ToDbDict(Arg)).All(Ct))
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
	>> FnPageByFKey(
		IDbFnCtx Ctx
		,ITable Tbl
		,CfgQry CfgQry
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
			var DbDict = Ctx.RunCmd(SqlCmd, Arg).IterAsyE(Ct);
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
		IUserCtx
		,IPageQry
		,CT
		,Task<IPageAsyE<PoWord>>
	>> FnPagePoWords(
		IDbFnCtx Ctx
		,CfgQry CfgQry
		,CT Ct
	){
		var T = TblMgr.GetTbl<PoWord>();
		var N = new PoWord.N();
		var POwner = T.Prm(N.Owner);
		var Sql =
$"""
SELECT * FROM {T.Qt(T.DbTblName)}
WHERE 1=1
{SqlFilterDel(T, CfgQry.IncludeDeleted)}
AND {T.Eq(POwner)}
ORDER BY {T.Fld(N.CreatedAt)} DESC
{T.SqlMkr.ParamLimOfst(out var PLmt, out var POfst)}
""";
		var SqlCmd = await Ctx.PrepareToDispose(SqlCmdMkr, Sql, Ct);
		var FnCnt = await RepoWord.FnCount(Ctx, Ct);
		return async(UserCtx, PageQry ,Ct)=>{
			var Arg = ArgDict.Mk(T)
			.AddT(POwner, UserCtx.UserId)
			.AddPageQry(PageQry, PLmt, POfst);

			var RawDbDicts = SqlCmd.Args(Arg).IterAsyE(Ct);
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

	//N+1 查詢問題
	public async Task<Func<
		IUserCtx
		,IPageQry
		,CT
		,Task<IPageAsyE<IJnWord>>
	>> FnPageWords(
		IDbFnCtx Ctx
		,CfgQry CfgQry
		,CT Ct
	){
		var TK = TblMgr.GetTbl<PoWordProp>();
		var TL = TblMgr.GetTbl<PoWordLearn>();
		var PagePoWords = await FnPagePoWords(Ctx, CfgQry, Ct);
		var PageKvByFKey = await FnPageByFKey(Ctx, TK, CfgQry, Ct);
		var PageLearnByFKey = await FnPageByFKey(Ctx, TL, CfgQry, Ct);

		return async(UserCtx, PageQry, Ct)=>{
			var PoWordsPage = await PagePoWords(UserCtx, PageQry, Ct);
			var R = PageAsyE<IJnWord>.Mk(PageQry, null, true, PoWordsPage.TotCnt);
			if(PoWordsPage.DataAsyE == null){
				return R;
			}
			var syncPoWordsPage = await PoWordsPage.ToSyncPage(Ct);//先全部載入內存、否則pg報錯不支持併發查

			async IAsyncEnumerable<IJnWord> fn(
				IEnumerable<PoWord> poWords
			){
				foreach(var PoWord in poWords){
					var KvPage = await PageKvByFKey(PoWord.Id, TK.PageSlctAll(), Ct);
					var syncKvPage = await KvPage.ToSyncPage(Ct);
					var Kvs = await _PageToList<PoWordProp>(syncKvPage, TK);

					var LearnPage = await PageLearnByFKey(PoWord.Id, TL.PageSlctAll(), Ct);
					var syncLearnPage = await LearnPage.ToSyncPage(Ct);
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
str NId = nameof(PoWord.Id)
	,NUpdateAt = nameof(PoWord.BizUpdatedAt),NStoredAt = nameof(PoWord.StoredAt)
;
var PTempus = T.Prm("Tempus");var POwner = T.Prm("Owner");

var Sql =
$"""
SELECT {T.Fld(NId)} AS {T.Qt(NId)}
FROM {T.Qt(T.DbTblName)}
WHERE 1=1
AND {T.Eq(POwner)}
AND (
	{T.Fld(NUpdateAt)} > {PTempus}
	OR {T.Fld(NStoredAt)} > {PTempus}
)
{T.SqlMkr.ParamLimOfst(out var Lmt, out var Ofst)}
""";//考慮同步後 一方ʃ新增、此旹無UpdatedAt 只有CreatedAt
		var SqlCmd = await SqlCmdMkr.Prepare(Ctx, Sql, Ct);
		Ctx?.AddToDispose(SqlCmd);
		return async(UserCtx, PageQry, Tempus, Ct)=>{
			var RawDictAsy = SqlCmd.WithCtx(Ctx).Args(ArgDict.Mk(T)
				.AddT(PTempus, Tempus)
				.AddT(POwner, UserCtx.UserId)
				.AddPageQry(PageQry, Lmt, Ofst)
			).IterAsyE(Ct);
			var WordIds = RawDictAsy.Select(x => T.RawToUpper<IdWord>(x[NId], NId));
			var R = PageAsyE<IdWord>.Mk(PageQry, WordIds);
			R.HasTotCnt = false;
			return R;
		};
	}

	void _(){

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
var Sql =
$"""
SELECT {N.Id} FROM {T.DbTblName}
WHERE 1=1
AND {T.Fld(N.Head)} LIKE {PPrefix} || '%'
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
			var RawDicts = SqlCmd.WithCtx(Ctx).Args(Arg).IterAsyE(Ct);
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
			var RawDicts = await SqlCmd.WithCtx(Ctx).Args(Arg).All(Ct);
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

}
