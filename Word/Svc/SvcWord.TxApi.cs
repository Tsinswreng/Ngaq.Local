namespace Ngaq.Local.Word.Svc;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Tools.Io;
using Ngaq.Local.Db;
using Ngaq.Core.Model.Word.Req;
using Ngaq.Core.Word.Models.Learn_;
using Tsinswreng.CsCore;
using Tsinswreng.CsPage;
using Ngaq.Core.Word.Models;
using Ngaq.Core.Infra;
using Ngaq.Core.Models.UserCtx;
using Ngaq.Core.Word.Models.Dto;
using Tsinswreng.CsTools;

public partial class SvcWord{
#region API

	public async Task<nil> UpdJnWord(IUserCtx User, JnWord JnWord, CT Ct){
		return await TxnWrapper.Wrap(FnUpdJnWord, User, JnWord, Ct);
	}

	[Impl]
	public async Task<IPage<JnWord>> SearchWord(
		IUserCtx User
		,IPageQry PageQry
		,ReqSearchWord Req
		,CT Ct
	){
		return await TxnWrapper.Wrap(FnSearchWord, User, PageQry, Req, Ct);
	}

	[Impl]
	public async Task<IPage<ITypedObj>> PageSearch(
		IUserCtx User
		,IPageQry PageQry
		,ReqSearchWord Req
		,CT Ct
	){
		return await TxnWrapper.Wrap(FnPageSearch, User, PageQry, Req, Ct);
	}

	[Impl]
	public async Task<nil> AddWordsFromFilePath(
		IUserCtx UserCtx
		,Path_Encode Path_Encode
		,CT Ct
	) {
		var Ctx = new DbFnCtx{Txn = await TxnGetter.GetTxnAsy(Ct)};
		var AddOrUpdateWords = await FnAddOrUpdWordsFromTxt(Ctx, Ct);
		await TxnRunner.RunTxn(Ctx.Txn, async(Ct)=>{
			var BoWords = await SvcParseWordList.ParseWordsFromFilePath(Path_Encode);
			await AddOrUpdateWords(UserCtx,BoWords,Ct);
			return NIL;
		}, Ct);

		return NIL;
	}

	[Impl]
	public async Task<nil> AddWordsFromText(
		IUserCtx UserCtx
		,string Text
		,CT Ct
	) {
		var Ctx = new DbFnCtx{Txn = await TxnGetter.GetTxnAsy(Ct)};
		var AddOrUpdateWords = await FnAddOrUpdWordsFromTxt(Ctx, Ct);
		await TxnRunner.RunTxn(Ctx.Txn, async(Ct)=>{
			var BoWords = await SvcParseWordList.ParseWordsFromText(Text,Ct);
			await AddOrUpdateWords(UserCtx,BoWords,Ct);
			return NIL;
		},Ct);
		return NIL;
	}


	[Impl]
	public async Task<nil> AddJnWords(
		IUserCtx UserCtx
		,IEnumerable<JnWord> JnWords
		,CT Ct
	){
		return await TxnWrapper.Wrap(FnMergeWordsIntoDb, UserCtx, JnWords, Ct);
	}

	[Impl]
	public async Task<nil> AddWordsFromUrl(
		IUserCtx UserCtx
		,string Path
		,CT Ct
	) {
		throw new NotImplementedException();
	}

	[Impl]
	public async Task<nil> AddWordId_PoLearnss(
		IUserCtx UserCtx
		,IEnumerable<WordId_PoLearns> WordId_PoLearnss
		,CT Ct
	){
		return await TxnWrapper.Wrap(FnAddWordId_PoLearnss, UserCtx, WordId_PoLearnss, Ct);
	}

	[Impl]
	public async Task<nil> AddWordId_LearnRecordss(
		IUserCtx UserCtx
		,IEnumerable<WordId_LearnRecords> WordId_LearnRecordss
		,CT Ct
	){
		var Ctx = new DbFnCtx{Txn = await TxnGetter.GetTxnAsy(Ct)};
		var AddWordId_PoLearnss = await FnAddWordId_PoLearnss(Ctx, Ct);
		return await TxnRunner.RunTxn(Ctx.Txn, async(Ct)=>{
			var WordId_PoLearns = WordId_LearnRecordss.Select(WordId_LearnRecords=>{
				var R = new WordId_PoLearns();
				R.PoLearns = WordId_LearnRecords.LearnRecords.Select(y=>y.ToPoLearn());
				R.WordId = WordId_LearnRecords.WordId;
				return R;
			});
			return await AddWordId_PoLearnss(UserCtx, WordId_PoLearns, Ct);
		},Ct);
	}

	[Impl]
	public async Task<IPage<JnWord>> PageJnWord(
		IUserCtx UserCtx
		,IPageQry PageQry
		,CT Ct
	){

		//var Ctx = new DbFnCtx{Txn = await GetTxnAsy.GetTxn()};
		var Ctx = new DbFnCtx();
		var Fn = await FnPageJnWords(Ctx, Ct);
		return await Fn(UserCtx, PageQry, Ct);
		//return await TxnWrapper.Wrap(FnPageJnWords, UserCtx, PageQry, Ct);//報錯曰The transaction object is not associated with the same connection object as this command.
	}

	[Impl]
	public async Task<nil> AddWordsByJsonLineIter(
		IUserCtx User
		,IAsyncEnumerable<str> JsonLineIter
		,CT Ct
	){
		return await TxnWrapper.Wrap(FnAddWordsByJsonLineIter, User, JsonLineIter, Ct);
	}

	[Impl]
	public async Task<nil> SoftDelJnWordsByIds(
		IUserCtx User
		,IEnumerable<IdWord> Ids
		,CT Ct
	){
		return await TxnWrapper.Wrap(FnSoftDelJnWordsByIds, User, Ids, Ct);
	}
	[Impl]
	public async Task<IPage<JnWord>> PageChangedWordsWithDelWordsAfterTime(
		IUserCtx User
		,IPageQry PageQry
		,Tempus Tempus
		,CT Ct
	){
		return await TxnWrapper.Wrap(FnPageChangedWordsWithDelWordsAfterTime, User, PageQry, Tempus, Ct);
	}
	#endregion API

}
