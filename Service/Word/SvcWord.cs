using Ngaq.Core.Infra.Core;
using Ngaq.Core.Infra.Db;
using Ngaq.Core.Model;
using Ngaq.Core.Model.Bo;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Model.UserCtx;
using Ngaq.Core.Model.Word.Dto;
using Ngaq.Core.Service.Word;
using Ngaq.Core.Tools;
using Ngaq.Core.Tools.Io;
using Ngaq.Local.Dao;
using Ngaq.Local.Db;
using Tsinswreng.SqlHelper.Cmd;
using Ngaq.Core.Infra.Page;

namespace Ngaq.Local.Service.Word;

//不在Svc中依賴DbCtx
public class SvcWord(
	ISvcParseWordList SvcParseWordList
	,ITxnRunner TxnRunner
	,DaoSqlWord DaoWord
	,IGetTxn GetTxnAsy
	,RepoSql<PoWord, IdWord> RepoPoWord
	,RepoSql<PoKv, IdKv> RepoKv
	,RepoSql<PoLearn, IdLearn> RepoLearn
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


	public async Task<Func<
		IUserCtx
		,IEnumerable<BoWord>
		,CancellationToken
		,Task<DuplicationGroup<BoWord>>
	>> FnGroupByExising(
		IDbFnCtx Ctx
		,CancellationToken ct
	){

		var SeekIdByFormEtLang = await DaoWord.FnSelectIdByHeadEtLang(Ctx, ct);
		var SeekBoWordById = await DaoWord.FnSelectBoWordById(Ctx, ct);
		var Fn = async(
			IUserCtx UserCtx
			,IEnumerable<BoWord> BoWords
			,CancellationToken ct
		)=>{


			var NonExistingList = new List<BoWord>();
			// var ExistingList = new List<BoWord>();
			// var DuplicatesList = new List<BoWord>();
			var ExiDupliPairs = new List<Existing_Duplication<BoWord>>();
			foreach(var BoWord in BoWords){
				var IdInDb = await SeekIdByFormEtLang(
					UserCtx
					,BoWord.PoWord.Head
					,BoWord.PoWord.Lang
					,ct
				);
				if(IdInDb == null){
					NonExistingList.Add(BoWord);
				}else{
					var BoWordInDb = await SeekBoWordById(IdInDb.Value, ct);
					if(BoWordInDb == null){
						throw new FatalLogicErr("BoWordInDb == null");
					}
					var ExiDupliPair = new Existing_Duplication<BoWord>(
						Existing: BoWordInDb
						,Duplication: BoWord
					);
					ExiDupliPairs.Add(ExiDupliPair);
					// BoWord.Id = ua.Value;
					// ExistingList.Add(BoWord);
				}
			}
			var R = new DuplicationGroup<BoWord>();
			R.Existing_Duplications = ExiDupliPairs;
			R.NonExistings = NonExistingList;
			return R;
		};
		return Fn;
	}

	public async Task<Func<
		IUserCtx
		,DtoAddWords
		,CancellationToken
		,Task<nil>
	>> FnAddOrUpdateWordsByDto(
		IDbFnCtx Ctx
		,CancellationToken Ct
	){
		var InsertBoWords = await DaoWord.FnInsertBoWords(Ctx, Ct);
		var InsertPoKvs = await DaoWord.FnInsertPoKvs(Ctx, Ct);
		var InsertPoLearns = await DaoWord.FnInsertPoLearns(Ctx, Ct);

		var Fn = async(
			IUserCtx UserCtx
			,DtoAddWords DtoAddWords
			,CancellationToken Ct
		)=>{
			using var NeoWords = new BatchListAsy<BoWord, nil>(InsertBoWords);
			using var NeoProps = new BatchListAsy<PoKv, nil>(InsertPoKvs);
			using var NeoLearns = new BatchListAsy<PoLearn, nil>(InsertPoLearns);

			//未加過之諸詞 加'add'ˉ學習記錄後直加入庫中則可
			foreach(var OneNonExisting in DtoAddWords.NeoWords){
				var Learn_Add = _MkLearn_Add();
				Learn_Add.FKeyUInt128 = OneNonExisting.Id;
				await NeoWords.Add(OneNonExisting, Ct);
				await NeoLearns.Add(Learn_Add, Ct);
			}

			// 有變動之諸新詞
			foreach(var UpdatedWord in DtoAddWords.UpdatedWords){
				if(UpdatedWord.DiffedProps.Count > 0){ //若NewProps則有變動、學習記錄添'add'
					var Learn_Add = _MkLearn_Add();
					Learn_Add.FKeyUInt128 = UpdatedWord.WordToAdd.Id;
					await NeoLearns.Add(Learn_Add, Ct);
					await NeoProps.AddRangeAsy(UpdatedWord.DiffedProps, Ct).FirstOrDefaultAsync();
				}
			}

			await NeoWords.End(Ct);
			await NeoProps.End(Ct);
			await NeoLearns.End(Ct);

			return Nil;
		};
		return Fn;
	}

/// <summary>
/// 蔿 待加之諸詞分類 按是否既存于庫中
/// </summary>
/// <param name="Ctx"></param>
/// <param name="ct"></param>
/// <returns></returns>
	public async Task<Func<
		IUserCtx
		,IEnumerable<BoWord>
		,CancellationToken
		,Task<DtoAddWords>
	>> FnClassifyWordsToAdd(
		IDbFnCtx Ctx
		,CancellationToken ct
	){
		var GroupByExisting = await FnGroupByExising(Ctx, ct);

		var Fn = async(
			IUserCtx UserCtx
			,IEnumerable<BoWord> BoWords
			,CancellationToken ct
		)=>{
			var R = new DtoAddWords();

			//按語言與詞頭分類
			var LangHead_Words = BoWords.GroupByLangHead();

			//合併後ʹ諸詞。斯列表中 同語言同詞頭之詞當只出現一次
			IList<BoWord> Mergeds = new List<BoWord>();
			foreach( var (LangHead, Words) in LangHead_Words ){
				var OneMerged = Words.MergeSameWords();
				if(OneMerged != null){
					Mergeds.Add(OneMerged);
				}
			}

			//查庫 篩出庫中既有ʹ舊詞 與 未加過之詞
			var ExistGroup = await GroupByExisting(UserCtx, Mergeds, ct);
			R.NeoWords = ExistGroup.NonExistings??[];

			// 有變動之諸新詞。
			var ChangedNewWords = new List<BoWord>();
			foreach(var Exi_Dupli in ExistGroup.Existing_Duplications??[]){
				var OldWord = Exi_Dupli.Existing;//庫中已有ʹ舊詞
				var NewWord = Exi_Dupli.Duplication;//待加ʹ新詞
				//待加ʹ新資產
				var NewProps = BoWord.DiffProps(NewWord.Props, OldWord.Props);
				var DtoUpdatedWord = new DtoUpdatedWord(
					WordInDb: OldWord
					,WordToAdd: NewWord
					,DiffedProps: NewProps
				);
				R.UpdatedWords.Add(DtoUpdatedWord);
			}
			return R;
		};
		return Fn;
	}

	// public async Task<Func<
	// 	IUserCtx
	// 	,IEnumerable<BoWord>
	// 	,CancellationToken
	// 	,Task<nil>
	// >> FnAddOrUpdateWords2(
	// 	IDbFnCtx Ctx
	// 	,CancellationToken ct
	// ){
	// 	var GroupByExisting = await FnGroupByExising(Ctx, ct);
	// 	var InsertBoWords = await DaoWord.FnInsertBoWords(Ctx, ct);
	// 	var InsertPoKvs = await DaoWord.FnInsertPoKvs(Ctx, ct);
	// 	var InsertPoLearns = await DaoWord.FnInsertPoLearns(Ctx, ct);

	// 	var Fn = async(
	// 		IUserCtx UserCtx
	// 		,IEnumerable<BoWord> BoWords
	// 		,CancellationToken ct
	// 	)=>{

	// 		//var R = new DuplicationGroup<>

	// 		using var NeoProps = new BatchListAsy<PoKv, nil>(InsertPoKvs);
	// 		using var NeoLearns = new BatchListAsy<PoLearn, nil>(InsertPoLearns);

	// 		//按語言與詞頭分類
	// 		var LangHead_Words = BoWords.GroupByLangHead();

	// 		//合併後ʹ諸詞。斯列表中 同語言同詞頭之詞當只出現一次
	// 		IList<BoWord> Mergeds = new List<BoWord>();
	// 		foreach( var (LangHead, Words) in LangHead_Words ){
	// 			var OneMerged = Words.MergeSameWords();
	// 			if(OneMerged != null){
	// 				Mergeds.Add(OneMerged);
	// 			}
	// 		}

	// 		//查庫 篩出庫中既有ʹ舊詞 與 未加過之詞
	// 		var ExistGroup = await GroupByExisting(UserCtx, Mergeds, ct);
	// 		//未加過之諸詞 加'add'ˉ學習記錄後直加入庫中則可
	// 		foreach(var OneNonExisting in ExistGroup.NonExistings??[]){
	// 			var Learn_Add = _MkLearn_Add();
	// 			OneNonExisting.AddLearn(Learn_Add);
	// 		}
	// 		await InsertBoWords(ExistGroup.NonExistings??[], ct);

	// 		// 有變動之諸新詞。
	// 		var ChangedNewWords = new List<BoWord>();
	// 		foreach(var Exi_Dupli in ExistGroup.Existing_Duplications??[]){
	// 			var OldWord = Exi_Dupli.Existing;//庫中已有ʹ舊詞
	// 			var NewWord = Exi_Dupli.Duplication;//待加ʹ新詞
	// 			//待加ʹ新資產
	// 			var NewProps = BoWord.DiffProps(NewWord.Props, OldWord.Props);
	// 			//若NewProps則有變動、學習記錄添'add'
	// 			if(NewProps.Count > 0){
	// 				var Learn_Add = OldWord.AddLearn(_MkLearn_Add());
	// 				await NeoLearns.Add(Learn_Add,ct);
	// 				foreach(var NewProp in NewProps){
	// 					NewProp.FKeyUInt128 = OldWord.Id;
	// 					await NeoProps.Add(NewProp, ct);
	// 				}
	// 			}
	// 		}
	// 		return Nil;
	// 	};
	// 	return Fn;
	// }

	PoLearn _MkLearn_Add(){
		var R = new PoLearn();
		R.SetStrToken(null, KeysProp.Inst.learn, ConstLearn.Inst.add);
		return R;
	}


	public async Task<Func<
		IUserCtx
		,IEnumerable<BoWord>
		,CancellationToken
		,Task<DtoAddWords>
	>> FnAddOrUpdateWords(
		IDbFnCtx Ctx
		,CancellationToken Ct
	){
		var ClassifyWordsToAdd = await FnClassifyWordsToAdd(Ctx, Ct);
		var AddOrUpdateWordsByDto = await FnAddOrUpdateWordsByDto(Ctx,Ct);
		var Fn = async(
			IUserCtx UserCtx
			,IEnumerable<BoWord> BoWords
			,CancellationToken Ct
		)=>{
			var DtoAddWords = await ClassifyWordsToAdd(UserCtx, BoWords, Ct);
			await AddOrUpdateWordsByDto(UserCtx, DtoAddWords, Ct);
			return DtoAddWords;
		};
		return Fn;
	}


	public async Task<Func<
		IUserCtx
		,IPageQuery
		,CancellationToken
		,Task<IPageAsy<BoWord>>
	>> FnPageBoWords(
		IDbFnCtx Ctx
		,CancellationToken Ct
	){
		return await DaoWord.FnPageBoWords(Ctx,Ct);
	}

	// [Obsolete("")]
	// public async Task<Func<
	// 	IUserCtx
	// 	,IEnumerable<BoWord>
	// 	,CancellationToken
	// 	,Task<nil>
	// >> FnAddOrUpdateWords(
	// 	IDbFnCtx Ctx
	// 	,CancellationToken ct
	// ){
	// 	var SeekIdByFormEtLang = await DaoWord.FnSelectIdByHeadEtLang(Ctx, ct);
	// 	var InsertBoWords = await DaoWord.FnInsertBoWords(Ctx, ct);
	// 	var InsertPoKvs = await DaoWord.FnInsertPoKvs(Ctx, ct);
	// 	var SelectBoWordById = await DaoWord.FnSelectBoWordById(Ctx, ct);
	// 	//TODO update `UpdateAt`
	// 	//var BatchSetUpdateAt = await Dao_Word.Fn_BatchSetUpdateAtAsy<Po_Word, Id_Word>(ct);
	// 	var Fn = async(
	// 		IUserCtx UserCtx
	// 		,IEnumerable<BoWord> BoWords
	// 		,CancellationToken ct
	// 	)=>{
	// 		u64 BatchSize = 0xfff;
	// 		using var NeoWords = new BatchListAsy<BoWord, nil>(InsertBoWords, BatchSize);
	// 		using var NeoProps = new BatchListAsy<PoKv, nil>(InsertPoKvs, BatchSize);

	// 		//TODO
	// 		using var ChangedPoWords = new BatchListAsy<PoWord, nil>(async (e,ct)=>{
	// 			//await BatchSetUpdateAt(e, DateTimeOffset.Now.ToUnixTimeMilliseconds() ,ct);
	// 			return Nil;
	// 		}, BatchSize);

	// 		foreach (var BoWord in BoWords) {//TODO 先去褈合併
	// 			SetPoWordOwner(UserCtx, BoWord.PoWord);
	// 			var ExistingId = await SeekIdByFormEtLang(
	// 				UserCtx
	// 				,BoWord.PoWord.Head
	// 				,BoWord.PoWord.Lang
	// 				,ct
	// 			);
	// 			var hasChanged = false;
	// 			if(ExistingId == null){//新詞
	// 				await NeoWords.Add(BoWord,ct);
	// 				hasChanged = true;
	// 			}else{//老詞
	// 				BoWord.Id = ExistingId.Value;
	// 				var OldBo_Word = await SelectBoWordById(ExistingId.Value, ct);
	// 				if(OldBo_Word == null){
	// 					throw new FatalLogicErr("failed to get old word");
	// 				}
	// 				var NewProps = BoWord.DiffProps(BoWord.Props, OldBo_Word.Props);
	// 				if(NewProps.Count == 0){
	// 					hasChanged = false;
	// 				}else{
	// 					foreach(var Po_Kv in NewProps){
	// 						Po_Kv.FKeyUInt128 = BoWord.Id.Value;
	// 						await NeoProps.Add(Po_Kv, ct);
	// 					}
	// 				}
	// 			}
	// 			if(hasChanged){
	// 				var Po_Learn = new PoLearn{
	// 					FKeyUInt128 = BoWord.Id.Value
	// 					//,KStr = Const_PropKey.
	// 					,VStr = Const_Learn.add
	// 				};
	// 				BoWord.Learns.Add(Po_Learn);
	// 				BoWord.PoWord.UpdatedAt = DateTimeOffset.Now.ToUnixTimeMilliseconds();
	// 				await ChangedPoWords.Add(BoWord.PoWord, ct);
	// 			}//~if(hasChanged)
	// 		}//~for
	// 		await NeoWords.End(ct);
	// 		await NeoProps.End(ct);
	// 		await ChangedPoWords.End(ct);
	// 		return Nil;
	// 	};
	// 	return Fn;
	// }

	public async Task<nil> AddWordsFromFilePath(
		IUserCtx UserCtx
		,Path_Encode Path_Encode
		,CancellationToken ct
	) {
		var Ctx = new DbFnCtx{Txn = await GetTxnAsy.GetTxn()};
		var AddOrUpdateWords = await FnAddOrUpdateWords(Ctx, ct);
		await TxnRunner.RunTxn(Ctx.Txn, async(ct)=>{
			var BoWords = await SvcParseWordList.ParseWordsFromFilePath(Path_Encode);
			await AddOrUpdateWords(UserCtx,BoWords,ct);
			return Nil;
		}, ct);

		return Nil;
	}

	public async Task<nil> AddWordsFromText(
		IUserCtx UserCtx
		,string Text
		,CancellationToken ct
	) {
		var Ctx = new DbFnCtx{Txn = await GetTxnAsy.GetTxn()};
		var AddOrUpdateWords = await FnAddOrUpdateWords(Ctx, ct);
		await TxnRunner.RunTxn(Ctx.Txn, async(ct)=>{
			var BoWords = await SvcParseWordList.ParseWordsFromText(Text,ct);
			await AddOrUpdateWords(UserCtx,BoWords,ct);
			return Nil;
		},ct);
		return Nil;
	}

	public async Task<nil> AddWordsFromUrl(
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

	public async Task<IPageAsy<BoWord>> PageBoWord(
		IUserCtx UserCtx
		,IPageQuery PageQry
		,CancellationToken Ct
	){

		//var Ctx = new DbFnCtx{Txn = await GetTxnAsy.GetTxn()};
		var Ctx = new DbFnCtx();
		var Fn = await FnPageBoWords(Ctx, Ct);
		return await Fn(UserCtx, PageQry, Ct);
		// var R = await TxnRunner.RunTxn(Ctx.Txn, async(Ct)=>{
		// 	return await Fn(UserCtx, PageQry, Ct);
		// },Ct);
		// return R;
	}
}
