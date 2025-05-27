using Ngaq.Core.Infra.Core;
using Ngaq.Core.Infra.Db;
using Ngaq.Core.Model.Bo;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Model.UserCtx;
using Ngaq.Core.Service.Word;
using Ngaq.Core.Tools;
using Ngaq.Core.Tools.Io;
using Ngaq.Local.Dao;
using Ngaq.Local.Db;
using Tsinswreng.SqlHelper.Cmd;

namespace Ngaq.Local.Service.Word;

//不在Svc中依賴DbCtx
public class SvcWord(
	ISvcParseWordList SvcParseWordList
	,ITxnRunner TxnRunner
	,DaoSqlWord DaoWord
	,IGetTxn GetTxnAsy
)
	: ISvcWord
{
	//public DbCtx DbCtx { get; set; } = DbCtx;

	public static PoWord SetPoWordOwner(
		IUserCtx UserCtx
		,PoWord PoWord
	){
		PoWord.Owner = UserCtx.UserId;
		return PoWord;
	}

	// public async Task<Func<
	// 	IUserCtx
	// 	,IEnumerable<BoWord>
	// 	,CancellationToken
	// 	,Task<ExistGroup<IdWord>>
	// >> {}


	//TODO 返 初添ʹ詞ʹ 及 更新ʹ詞ʹID
	public async Task<Func<
		IUserCtx
		,IEnumerable<BoWord>
		,CancellationToken
		,Task<nil>
	>> FnAddOrUpdateWordsAsy(
		IDbFnCtx Ctx
		,CancellationToken ct
	){
		var SeekIdByFormEtLang = await DaoWord.FnSelectIdByFormIdEtLang(Ctx, ct);
		var InsertBoWords = await DaoWord.FnInsertBoWordsAsy(Ctx, ct);
		var InsertPoKvs = await DaoWord.FnInsertPoKvsAsy(Ctx, ct);
		var SelectBoWordById = await DaoWord.FnSelectBoWordById(Ctx, ct);
		//TODO update `UpdateAt`
		//var BatchSetUpdateAt = await Dao_Word.Fn_BatchSetUpdateAtAsy<Po_Word, Id_Word>(ct);
		var Fn = async(
			IUserCtx UserCtx
			,IEnumerable<BoWord> BoWords
			,CancellationToken ct
		)=>{
			u64 BatchSize = 0xfff;
			using var NeoWords = new BatchListAsy<BoWord, nil>(InsertBoWords, BatchSize);
			using var NeoProps = new BatchListAsy<PoKv, nil>(InsertPoKvs, BatchSize);

			//TODO
			using var ChangedPoWords = new BatchListAsy<PoWord, nil>(async (e,ct)=>{
				//await BatchSetUpdateAt(e, DateTimeOffset.Now.ToUnixTimeMilliseconds() ,ct);
				return Nil;
			}, BatchSize);

			foreach (var BoWord in BoWords) {//TODO 先去褈合併
				SetPoWordOwner(UserCtx, BoWord.PoWord);
				var ExistingId = await SeekIdByFormEtLang(
					UserCtx
					,BoWord.PoWord.WordFormId
					,BoWord.PoWord.Lang
					,ct
				);
				var hasChanged = false;
				if(ExistingId == null){//新詞
					await NeoWords.AddAsy(BoWord,ct);
					hasChanged = true;
				}else{//老詞
					BoWord.Id = ExistingId.Value;
					var OldBo_Word = await SelectBoWordById(ExistingId.Value, ct);
					if(OldBo_Word == null){
						throw new FatalLogicErr("failed to get old word");
					}
					var NewProps = BoWord.DiffProps(BoWord.Props, OldBo_Word.Props);
					if(NewProps.Count == 0){
						hasChanged = false;
					}else{
						foreach(var Po_Kv in NewProps){
							Po_Kv.FKeyUInt128 = BoWord.Id.Value;
							await NeoProps.AddAsy(Po_Kv, ct);
						}
					}
				}
				if(hasChanged){
					var Po_Learn = new PoLearn{
						FKeyUInt128 = BoWord.Id.Value
						//,KStr = Const_PropKey.
						,VStr = ConstLearn.add
					};
					BoWord.Learns.Add(Po_Learn);
					BoWord.PoWord.UpdatedAt = DateTimeOffset.Now.ToUnixTimeMilliseconds();
					await ChangedPoWords.AddAsy(BoWord.PoWord, ct);
				}//~if(hasChanged)
			}//~for
			await NeoWords.EndAsy(ct);
			await NeoProps.EndAsy(ct);
			await ChangedPoWords.EndAsy(ct);
			return Nil;
		};
		return Fn;
	}


	public async Task<nil> AddWordsFromFilePathAsy(
		IUserCtx UserCtx
		,Path_Encode Path_Encode
		,CancellationToken ct
	) {
		var Ctx = new DbFnCtx{Txn = await GetTxnAsy.GetTxn()};
		var AddOrUpdateWords = await FnAddOrUpdateWordsAsy(Ctx, ct);
		await TxnRunner.RunTxn(Ctx.Txn, async(ct)=>{
			var BoWords = await SvcParseWordList.ParseWordsFromFilePathAsy(Path_Encode);
			await AddOrUpdateWords(UserCtx,BoWords,ct);
			return Nil;
		}, ct);
		// await AddOrUpdateWords(UserCtx,BoWords,ct);
		// await Ctx.Txn.CommitAsy(ct);
		// await TxnAsyFnRunner.TxnAsy(async(ct)=>{
		// 	return await AddOrUpdateWords(UserCtx,BoWords,ct);
		// }, ct);
		return Nil;
	}

	public async Task<nil> AddWordsFromTextAsy(
		IUserCtx UserCtx
		,string Text
		,CancellationToken ct
	) {
		var Ctx = new DbFnCtx{Txn = await GetTxnAsy.GetTxn()};
		var AddOrUpdateWords = await FnAddOrUpdateWordsAsy(Ctx, ct);
		await TxnRunner.RunTxn(Ctx.Txn, async(ct)=>{
			var BoWords = await SvcParseWordList.ParseWordsFromTextAsy(Text,ct);
			await AddOrUpdateWords(UserCtx,BoWords,ct);
			return Nil;
		},ct);
		//await Ctx.Txn.CommitAsy(ct);
		// await TxnAsyFnRunner.TxnAsy(async(ct)=>{
		// 	return await AddOrUpdateWords(UserCtx,BoWords,ct);
		// }, ct);
		return Nil;
	}

	public async Task<nil> AddWordsFromUrlAsy(
		IUserCtx UserCtx
		,string Path
		,CancellationToken ct
	) {
		throw new NotImplementedException();
		// var AddOrUpdateWords = await Fn_AddOrUpdateWordsAsy(ct);
		// var BoWords = await Svc_ParseWordList.ParseWordsFromFilePathAsy(Path_Encode);
		// return await TxnAsyFnRunner.TxnAsy(async(ct)=>{
		// 	return await AddOrUpdateWords(UserCtx,BoWords,ct);
		// }, ct);
		//return Nil;
	}
}
