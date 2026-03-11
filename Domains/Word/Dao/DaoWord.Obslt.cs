using Ngaq.Core.Infra.IF;
using Ngaq.Core.Shared.User.UserCtx;
using Ngaq.Core.Shared.Word.Models;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Shared.Word.Models.Po.Learn;
using Ngaq.Core.Shared.Word.Models.Po.Word;
using Ngaq.Local.Db.TswG;
using Tsinswreng.CsSqlHelper;
using Tsinswreng.CsTools;

namespace Ngaq.Local.Word.Dao;
using IStr_Any = System.Collections.Generic.IDictionary<str, obj?>;
using Str_Any = System.Collections.Generic.Dictionary<str, obj?>;
public partial class DaoWord{
	
	
[Obsolete]
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
	
	[Obsolete(@$"用{nameof(BatSlctIdByOwnerHeadLangWithDel)}")]
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
			
			var ans = GotDict[T.DbCol(x=>x.Id)];
			return IdWord.FromByteArr((u8[])ans!);
		};
	}

	
	/// <delete>
	/// /// 予Svc層調用、不在Dao層調用。
	/// Dao層之操作更低級、直ᵈ操作數據庫、寡聚合、不作額外校驗
	/// 如FnInsertPoKvs中、內ʹ函數ʹ參數ˋ只受IEnumerable<PoWordProp>、未必潙同一詞ᐪ。且初加旹亦蜮調此 洏初加旹不璫更新UpdatedAt
	/// </delete>
	[Obsolete]
	public async Task<Func<
		IdWord
		,CT
		,Task<nil>
	>> FnTriggerOnRootAfterUpd(IDbFnCtx Ctx, CT Ct){
		var UpdPoWord = await RepoWord.AsAppRepo().FnUpd_BizUpdatedAt(Ctx,Ct);
		return async(WordId, Ct)=>{
			await UpdPoWord(WordId, Ct);
			return NIL;
		};
	}
	
	[Obsolete]
	public async Task<Func<
		IdWord?
		,IEnumerable<PoWordLearn>
		,CT
		,Task<nil>
	>> FnInsertPoLearns(
		IDbFnCtx Ctx
		,CT Ct
	){
		var UpdUpd = await FnTriggerOnRootAfterUpd(Ctx, Ct);
		var InsertMany = await RepoLearn.FnInsertMany(Ctx, Ct);
		return async(WordId, PoLearns, Ct)=>{
			await InsertMany(PoLearns, Ct);
			if(WordId is not null){
				await UpdUpd(WordId.Value, Ct);
			}
			return NIL;
		};
	}
	[Obsolete]
	public async Task<Func<
		IdWord? //斯批PoWordProp 屬于哪個詞
		,IEnumerable<PoWordProp>
		,CT
		,Task<nil>
	>> FnInsertPoKvs(
		IDbFnCtx Ctx
		,CT Ct
	){
		var UpdUpd = await FnTriggerOnRootAfterUpd(Ctx,Ct);
		var InsertMany = await RepoProp.FnInsertMany(Ctx, Ct);
		return async(WordId,PoKvs,Ct)=>{
			await InsertMany(PoKvs, Ct);
			if(WordId is not null){
				await UpdUpd(WordId.Value, Ct);
			}
			return NIL;
		};
	}
	
	[Obsolete("用Bat")]
	public async Task<Func<
		IEnumerable<IJnWord>
		,CT
		,Task<nil>
	>> FnInsertJnWords(
		IDbFnCtx Ctx
		,CT Ct
	) {
		var InsertPoWords = await RepoWord.FnInsertMany(Ctx, Ct);
		var InsertPoKvs = await RepoProp.FnInsertMany(Ctx, Ct);
		var InsertPoLearns = await RepoLearn.FnInsertMany(Ctx, Ct);

		return async(JnWords, Ct)=>{
			u64 BatchSize = 0xfff;
			await using var PoWords = new BatchCollector<PoWord, nil>(async(list, ct)=>{
				await InsertPoWords(list,ct);
				return NIL;
			}, BatchSize);
			await using var PoKvs = new BatchCollector<PoWordProp, nil>(async(e, ct)=>{
				await InsertPoKvs(e, ct);
				return NIL;
			}, BatchSize);
			await using var PoLearns = new BatchCollector<PoWordLearn, nil>(async(e, ct)=>{
				await InsertPoLearns(e, ct);
				return NIL;
			}, BatchSize);
			foreach (var JWord in JnWords) {
				JWord.EnsureForeignId();
				await PoWords.Add(JWord.Word, Ct);
				foreach (var Prop in JWord.Props) {
					await PoKvs.Add(Prop, Ct);
				}
				foreach (var Learn in JWord.Learns) {
					await PoLearns.Add(Learn, Ct);
				}
			}
			await PoWords.End(Ct);
			await PoKvs.End(Ct);
			await PoLearns.End(Ct);
			return NIL;
		};
	}

}
