namespace Ngaq.Local.Word.Dao;
using Ngaq.Core.Infra;
using Ngaq.Core.Model.Po;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Local.Db;
using Tsinswreng.CsSqlHelper;
using Str_Any = System.Collections.Generic.Dictionary<str, obj?>;
using IStr_Any = System.Collections.Generic.IDictionary<str, obj?>;
using Ngaq.Core.Infra.Errors;
using Ngaq.Core.Models.Po;
using Ngaq.Core.Word.Models.Po.Learn;
using Tsinswreng.CsPage;
using Ngaq.Core.Word.Models;
using Ngaq.Core.Models.UserCtx;
using Tsinswreng.CsTools;
using Ngaq.Local.Db.TswG;
using Ngaq.Core.Model.Sys.Po.User;
using System.Diagnostics;
using Ngaq.Core.Word.Models.Dto;
using Ngaq.Core.Model.UserCtx;
using Ngaq.Core.Word.Models.Po.Word;
using Ngaq.Core.Word.Models.Po.Kv;
using System.Linq.Expressions;

public partial class DaoSqlWord{
	public async Task<Func<
		IUserCtx,
		str,//Head
		str,//Lang
		CT
		,Task<IdWord?>
	>>
	FnSlctIdByOwnerHeadLang(IDbFnCtx Ctx,CT Ct){
		var T = TblMgr.GetTbl<PoWord>();
		var NId = nameof(PoWord.Id); var NOwner = nameof(PoWord.Owner); var NHead = nameof(PoWord.Head); var NLang = nameof(PoWord.Lang);
		var POwner = T.Prm(NOwner);var PHead = T.Prm(NHead);
		var PLang = T.Prm(NLang);
		var L = T.UpperToRaw;
		var Sql =
$"""
SELECT {T.Fld(NId)} FROM {T.Qt(T.DbTblName)}
WHERE 1=1
AND {T.Fld(nameof(IPoBase.DelId))} IS NULL
AND {T.Fld(NOwner)} = {POwner}
AND {T.Fld(NHead)} = {PHead}
AND {T.Fld(NLang)} = {PLang}
""";
		var SqlCmd = await SqlCmdMkr.Prepare(Ctx, Sql, Ct);
		return async (User,Head,Lang,Ct)=>{
			var UserId = User.UserId;
			var Args = ArgDict.Mk()
			.Add(POwner, L(UserId, NOwner))
			.Add(PHead, L(Head, NHead))
			.Add(PLang, L(Lang, NLang));
			var GotDict = await SqlCmd.Args(Args).IterIAsy(Ct).FirstOrDefaultAsync(Ct);
			if(GotDict == null){
				return null;
			}
			var ans = GotDict[T.ColNameToDb(NId)];
			return IdWord.FromByteArr((u8[])ans);
		};
	}

	public async Task<Func<
		IdWord
		,CT
		,Task<JnWord?>
	>> FnSlctJnWordById(
		IDbFnCtx? Ctx
		,CT Ct
	){
		var TW = TblMgr.GetTbl<PoWord>();
		var TK = TblMgr.GetTbl<PoWordProp>();
		var TL = TblMgr.GetTbl<PoWordLearn>();
		var NWordId = nameof(PoWordProp.WordId);
		var Sql_SeekByFKey = (str QuotedTblName)=>{
			var Sql =
$"""
SELECT * FROM {QuotedTblName}
WHERE 1=1
AND {TK.Fld(nameof(IPoBase.DelId))} IS NULL
AND {TK.Fld(NWordId)} = {TW.Prm(NWordId)}
""";
			return Sql;
		};
		var GetPoWordById = await RepoWord.FnSlctById(Ctx, Ct);
		var Cmd_SeekKv = await SqlCmdMkr.Prepare(Ctx, Sql_SeekByFKey(TK.Qt(TK.DbTblName)), Ct);
		var Cmd_SeekLearn = await SqlCmdMkr.Prepare(Ctx, Sql_SeekByFKey(TL.Qt(TL.DbTblName)), Ct);

		return async(Id,Ct)=>{
			var PoWord = await GetPoWordById(Id, Ct);
			if(PoWord == null){
				return null;
			}
			if(PoWord.IsDeleted()){
				return null;
			}

			var Arg = new Str_Any{
				[nameof(PoWordProp.WordId)] = Id
			};
			var RawPropDicts = (await Cmd_SeekKv.RawArgs(TK.ToDbDict(Arg)).All(Ct))
				.Select(dbDict=>TK.DbDictToEntity<PoWordProp>(dbDict))
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


	public async Task<Func<
		IdWord
		,IPageQry
		,CT
		,Task<IPage<IStr_Any>>
	>> FnPageByFKey(
		IDbFnCtx Ctx
		,ITable Tbl
		,CT Ct
	){
		var T = Tbl;
		var TW = TblMgr.GetTbl<PoWord>();
		var NWordId = nameof(I_WordId.WordId);
		var Sql =
$"""
SELECT * FROM {Tbl.Qt(Tbl.DbTblName)}
WHERE {Tbl.Fld(NWordId)} = {Tbl.Prm(NWordId)}
AND {Tbl.Fld(nameof(IPoBase.DelId))} IS NULL
{Tbl.SqlMkr.ParamLimOfst(out var PLmt, out var POfst)}
""";
		var SqlCmd = await SqlCmdMkr.Prepare(Ctx, Sql, Ct);
		var Fn = async(
			IdWord IdWord
			,IPageQry PageQry
			,CT Ct
		)=>{

			var Arg = ArgDict.Mk()
			.Add(NWordId, TW.UpperToRaw(IdWord))
			.AddPageQry(PageQry, PLmt, POfst);
			var DbDict = await SqlCmd.Args(Arg).All(Ct);
			u64 Cnt = 0;
			//if(PageQry.HasTotalCount){Cnt = await FnCnt(Ct);}
			IPage<IStr_Any> R = new Page<IStr_Any>{
				PageQry=PageQry,
				TotCnt=Cnt,
				Data=DbDict,
			};
			return R;
		};
		return Fn;

	}

	public async Task<Func<
		IUserCtx
		,IPageQry
		,CT
		,Task<IPage<PoWord>>
	>> FnPagePoWords(
		IDbFnCtx Ctx
		,CT Ct
	){
		var TW = TblMgr.GetTbl<PoWord>();
		var N = new PoWord.N();
		var POwner = TW.Prm(N.Owner);
		var Sql =
$"""
SELECT * FROM {TW.Qt(TW.DbTblName)}
WHERE {TW.Fld(N.Owner)} = {POwner}
AND {TW.Fld(N.DelId)} IS NULL
ORDER BY {TW.Fld(N.CreatedAt)} DESC
{TW.SqlMkr.ParamLimOfst(out var PLmt, out var POfst)}
""";
		var SqlCmd = await SqlCmdMkr.Prepare(Ctx, Sql, Ct);
		Ctx?.AddToDispose(SqlCmd);
		var FnCnt = await RepoWord.FnCount(Ctx, Ct);
		return async(UserCtx, PageQry ,Ct)=>{
			var Arg = ArgDict.Mk()
			.Add(POwner, TW.UpperToRaw(UserCtx.UserId))
			.AddPageQry(PageQry, PLmt, POfst);

			var RawDbDicts = await SqlCmd.Args(Arg).All(Ct);
			var PoWords = RawDbDicts.Select(
				(Raw)=>TW.DbDictToEntity<PoWord>(Raw)
			).ToListTryNoCopy();
			var Cnt = PageQry.WantTotCnt?  await FnCnt(Ct)  :  0;
			IPage<PoWord> R = new Page<PoWord>{
				PageQry = PageQry,
				TotCnt = Cnt,
				Data = PoWords
			};
			return R;
		};
	}

	public async Task<Func<
		IUserCtx
		,IPageQry
		,CT
		,Task<IPage<JnWord>>
	>> FnPageJnWords(
		IDbFnCtx Ctx
		,CT Ct
	){
		var TK = TblMgr.GetTbl<PoWordProp>();
		var TL = TblMgr.GetTbl<PoWordLearn>();
		var PagePoWords = await FnPagePoWords(Ctx, Ct);
		var PageKvByFKey = await FnPageByFKey(Ctx, TK, Ct);
		var PageLearnByFKey = await FnPageByFKey(Ctx, TL, Ct);

		var Fn = async(
			IUserCtx UserCtx
			,IPageQry PageQry
			,CT Ct
		)=>{
			var PoWordsPage = await PagePoWords(UserCtx, PageQry, Ct);
			var R = Page<JnWord>.Mk(PageQry, null, true, PoWordsPage.TotCnt);
			if(PoWordsPage.Data == null){
				return R;
			}

			var JnWordsTasks = PoWordsPage.Data.Select(async (PoWord)=>{
				var KvPage = await PageKvByFKey(PoWord.Id, Tsinswreng.CsPage.PageQry.SlctAll(), Ct);
				var Kvs = await _PageToList<PoWordProp>(KvPage, TK);

				var LearnPage = await PageLearnByFKey(PoWord.Id, Tsinswreng.CsPage.PageQry.SlctAll(), Ct);
				var Learns = await _PageToList<PoWordLearn>(LearnPage, TL);

				var R = new JnWord(PoWord, Kvs, Learns);
				return R;
			});
			R.Data = await Task.WhenAll(JnWordsTasks);
			return R;
		};
		return Fn;
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
		,Task<IPage<IdWord>>
	>> FnPageChangedWordIdsWithDelWordsAfterTime(IDbFnCtx Ctx, CT Ct){
var T = TblMgr.GetTbl<PoWord>();
str NId = nameof(PoWord.Id),NOwner = nameof(PoWord.Owner)
	,NUpdateAt = nameof(PoWord.UpdatedAt),NStoredAt = nameof(PoWord.StoredAt)
;
var PTempus = T.Prm("Tempus");var POwner = T.Prm("Owner");

var Sql =
$"""
SELECT {T.Fld(NId)} AS {T.Qt(NId)}
FROM {T.Qt(T.DbTblName)}
WHERE {T.Fld(NOwner)} = {POwner}
AND (
	{T.Fld(NUpdateAt)} > {PTempus}
	OR {T.Fld(NStoredAt)} > {PTempus}
)
{T.SqlMkr.ParamLimOfst(out var Lmt, out var Ofst)}
""";//考慮同步後 一方ʃ新增、此旹無UpdatedAt 只有CreatedAt
		var SqlCmd = await SqlCmdMkr.Prepare(Ctx, Sql, Ct);
		Ctx?.AddToDispose(SqlCmd);
		return async(UserCtx, PageQry, Tempus, Ct)=>{
			var RawDictAsy = await SqlCmd.WithCtx(Ctx).RawArgs(ArgDict.Mk()
				.Add(PTempus, T.UpperToRaw(Tempus))
				.Add(POwner, T.UpperToRaw(UserCtx.UserId))
				.AddPageQry(PageQry, Lmt, Ofst)
				.ToDict()
			).All(Ct);
			var WordIds = RawDictAsy.Select(x => T.RawToUpper<IdWord>(x[NId], NId)).ToList();
			var R = Page<IdWord>.Mk(PageQry, WordIds);
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
	>> FnPageSearchWordIdsByHeadPrefix(IDbFnCtx Ctx, CT Ct){
var T = TblMgr.GetTbl<PoWord>();
var N = new PoWord.N();
var PPrefix = T.Prm("Prefix"); var POwner = T.Prm(N.Owner); var PLang = T.Prm(N.Lang);
var Sql =
$"""
SELECT {N.Id} FROM {T.DbTblName}
WHERE 1=1
AND {T.Fld(N.DelId)} IS NULL
AND {T.Fld(N.Head)} LIKE {PPrefix} || '%'
AND {T.Fld(N.Owner)} = {POwner}
ORDER BY {T.Fld(N.Head)} ASC
{T.SqlMkr.ParamLimOfst(out var PLmt, out var POfst)}
"""; // AND {T.Fld(NLang)} = {PLang}
		var SqlCmd = await SqlCmdMkr.Prepare(Ctx, Sql, Ct);
		Ctx?.AddToDispose(SqlCmd);
		return async(User, PageQry, Req, Ct)=>{
			var Arg = ArgDict.Mk(T)
			.Add(PPrefix, Req.RawStr)
			.Add(PLang, Req.Lang)
			.AddConv(POwner, User.UserId)
			.AddPageQry(PageQry, PLmt, POfst)
			;
			var RawDicts = SqlCmd.WithCtx(Ctx).Args(Arg).IterIAsy(Ct);
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
WHERE {Tbl.Fld(NId)} = {PId}
""";
		var SqlCmd = await SqlCmdMkr.Prepare(Ctx, Sql, Ct);
		Ctx?.AddToDispose(SqlCmd);
		return async (Id, Ct)=>{
			var Arg = ArgDict.Mk()
			.Add(PId, Tbl.UpperToRaw(Id, NId));
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
