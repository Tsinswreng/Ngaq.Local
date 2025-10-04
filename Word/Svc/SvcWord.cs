using Ngaq.Core.Infra.Core;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Model.Word.Dto;
using Ngaq.Core.Service.Word;
using Ngaq.Core.Tools.Io;
using Ngaq.Local.Db;
using Ngaq.Core.Infra.Errors;
using Tsinswreng.CsTools;
using Ngaq.Core.Model.Word.Req;
using Ngaq.Core.Word.Models.Learn_;
using Tsinswreng.CsSqlHelper;
using Ngaq.Core.Word.Models.Po.Learn;
using Ngaq.Core.Word.Svc;
using Tsinswreng.CsCore;
using Tsinswreng.CsPage;
using Ngaq.Core.Word.Models;
using Ngaq.Core.Infra;
using Ngaq.Core.Models.UserCtx;
using Ngaq.Core.Models;
using Ngaq.Local.Word.Dao;
using Ngaq.Local.Db.TswG;
using Ngaq.Core.Tools.Json;
using Ngaq.Core.Word.Models.Dto;
using Ngaq.Core.Word.Models.Po.Word;
using Ngaq.Core.Word.Models.Po.Kv;

namespace Ngaq.Local.Word.Svc;

//不在Svc中依賴DbCtx
public partial class SvcWord(
	ISvcParseWordList SvcParseWordList
	,ITxnRunner TxnRunner
	,DaoSqlWord DaoWord
	,I_GetTxnAsy TxnGetter
	,IAppRepo<PoWord, IdWord> RepoPoWord
	,IAppRepo<PoWordProp, IdWordProp> RepoKv
	,IAppRepo<PoWordLearn, IdWordLearn> RepoLearn
	,TxnWrapper<DbFnCtx> TxnWrapper
	,IJsonSerializer JsonSerializer
)
	: ISvcWord
{


	public partial class EErr_:EnumErr{
		public IAppErr WordOwnerNotMatch() => Mk(nameof(WordOwnerNotMatch));
	}
	public EErr_ EErr = new EErr_();


	public static PoWord SetPoWordOwner(
		IUserCtx UserCtx
		,PoWord PoWord
	){
		PoWord.Owner = UserCtx.UserId;
		return PoWord;
	}


	public async Task<Func<
		IUserCtx
		,IEnumerable<JnWord>
		,CT
		,Task<DuplicationGroup<JnWord>>
	>> FnGroupByExising(
		IDbFnCtx Ctx
		,CT Ct
	){
		var SeekIdByHeadEtLang = await DaoWord.FnSlctIdByOwnerHeadLang(Ctx, Ct);
		var SeekJnWordById = await DaoWord.FnSlctJnWordById(Ctx, Ct);
		return async(UserCtx, JnWords, Ct)=>{
			var NonExistingList = new List<JnWord>();
			var ExiDupliPairs = new List<Existing_Duplication<JnWord>>();
			foreach(var (i,JnWord) in JnWords.Index()){//慢
				var IdInDb = await SeekIdByHeadEtLang(
					UserCtx
					,JnWord.Word.Head
					,JnWord.Word.Lang
					,Ct
				);
				if(IdInDb == null){
					NonExistingList.Add(JnWord);
				}else{
					var JnWordInDb = await SeekJnWordById(IdInDb.Value, Ct);
					if(JnWordInDb == null){
						throw new FatalLogicErr("BoWordInDb == null");
					}
					var ExiDupliPair = new Existing_Duplication<JnWord>(
						Existing: JnWordInDb
						,Duplication: JnWord
					);
					ExiDupliPairs.Add(ExiDupliPair);
				}
			}
			var R = new DuplicationGroup<JnWord>();
			R.Existing_Duplications = ExiDupliPairs;
			R.NonExistings = NonExistingList;
			return R;
		};
	}


#if false
/// <summary>
	/// 專用于添詞芝從文本詞表
	/// </summary>
	/// <param name="Ctx"></param>
	/// <param name="Ct"></param>
	/// <returns></returns>
	[Obsolete]
	public async Task<Func<
		IUserCtx
		,DtoAddWords
		,CT
		,Task<nil>
	>> FnAddOrUpdWordsFromTxtByDto(
		IDbFnCtx Ctx
		,CT Ct
	){
		var InsertJnWords = await DaoWord.FnInsertJnWords(Ctx, Ct);
		var InsertPoKvs = await DaoWord.FnInsertPoKvs(Ctx, Ct);
		var InsertPoLearns = await DaoWord.FnInsertPoLearns(Ctx, Ct);
		var UpdUpd = await RepoPoWord.FnUpd_UpdatedAt(Ctx,Ct);

		var Fn = async(
			IUserCtx UserCtx
			,DtoAddWords DtoAddWords
			,CT Ct
		)=>{
			await using var NeoWords = new BatchListAsy<JnWord, nil>(InsertJnWords);
			await using var NeoProps = new BatchListAsy<PoWordProp, nil>(InsertPoKvs);
			await using var NeoLearns = new BatchListAsy<PoWordLearn, nil>(InsertPoLearns);

			//未加過之諸詞 加'add'ˉ學習記錄後直加入庫中則可
			foreach(var OneNonExisting in DtoAddWords.NeoWords){
				OneNonExisting.StoredAt = Tempus.Now();
				var NeoPoLearns = MkPoLearns(OneNonExisting.Props, OneNonExisting.Id);
				await NeoWords.Add(OneNonExisting, Ct);
				await NeoLearns.AddMany(NeoPoLearns, null, Ct);
			}

			// 有變動之諸新詞
			foreach(var UpdatedWord in DtoAddWords.UpdatedWords){
				// if(UpdatedWord.DiffedProps.Count <= 0){
				// 	continue;
				// }
				if(UpdatedWord.DiffedWord == null){
					continue;
				}
				//若NewProps則有變動、學習記錄添'add'
				var NeoPoLearns = MkPoLearns(UpdatedWord.DiffedWord.Props, UpdatedWord.WordInDb.Id);
				await NeoLearns.AddMany(NeoPoLearns, null, Ct);
				UpdatedWord.DiffedWord.Props = UpdatedWord.DiffedWord.Props.Select(x=>{
					x.WordId = UpdatedWord.WordInDb.Id;
					return x;
				}).ToList();
				await NeoProps.AddMany(UpdatedWord.DiffedWord.Props, null, Ct);
				await UpdUpd(UpdatedWord.WordInDb.Id, Ct);
			}

			await NeoWords.End(Ct);
			await NeoProps.End(Ct);
			await NeoLearns.End(Ct);

			return NIL;
		};
		return Fn;
	}
#endif

/// <summary>
/// 蔿 生詞表文本ᙆ待加之諸詞分類 按是否既存于庫中
/// </summary>
/// <returns></returns>
	public async Task<Func<
		IUserCtx
		,IEnumerable<JnWord>
		,CT
		,Task<DtoAddWords>
	>> FnClassifyWordsToAdd(
		IDbFnCtx Ctx
		,CT Ct
	){
		var GroupByExisting = await FnGroupByExising(Ctx, Ct);

		var Fn = async(
			IUserCtx UserCtx
			,IEnumerable<JnWord> JnWords
			,CT Ct
		)=>{
			var R = new DtoAddWords();

			//按語言與詞頭分類
			var HeadLang_Words = JnWords.GroupByLangHead();

			//合併後ʹ諸詞。斯列表中 同語言同詞頭之詞當只出現一次
			IList<JnWord> Mergeds = new List<JnWord>();
			foreach( var (HeadLang, Words) in HeadLang_Words ){
				var OneMerged = Words.NoDiffMergeSameWords();
				if(OneMerged != null){
					Mergeds.Add(OneMerged);
				}
			}

			//查庫 篩出庫中既有ʹ舊詞 與 未加過之詞
			var ExistGroup = await GroupByExisting(UserCtx, Mergeds, Ct);
			R.NeoWords = ExistGroup.NonExistings??[];

			// 有變動之諸新詞。
			var ChangedNewWords = new List<JnWord>();
			foreach(var Exi_Dupli in ExistGroup.Existing_Duplications??[]){
				var OldWord = Exi_Dupli.Existing;//庫中已有ʹ舊詞
				var NewWord = Exi_Dupli.Duplication;//待加ʹ新詞

				// //待加ʹ新資產
				// var NewProps = JnWord.DiffProps(NewWord.Props, OldWord.Props);
				// var DtoUpdatedWord = new DtoUpdWord(
				// 	WordInDb: OldWord
				// 	,WordToAdd: NewWord
				// 	,DiffedProps: NewProps
				// );
				var Diffed = NewWord.Diff(OldWord);
				if(Diffed == null){
					continue;
				}
				var DtoUpdatedWord = new DtoUpdWord(
					WordInDb: OldWord
					,WordToAdd: NewWord
					,DiffedWord: Diffed
				);

				R.UpdatedWords.Add(DtoUpdatedWord);
			}
			return R;
		};
		return Fn;
	}


	protected IEnumerable<PoWordLearn> MkPoLearns(IEnumerable<PoWordProp> NeoProps, IdWord WordId){
		foreach(var Prop in NeoProps){
			if(Prop.KStr == KeysProp.Inst.description){
				var U = new PoWordLearn();
				U.CreatedAt = Prop.CreatedAt;
				U.LearnResult = ELearn.Add;
				U.WordId = WordId;
				yield return U;
			}
		}
	}


/// <summary>
/// 把DtoAddWords 寫入數據庫。不類[從文本生詞表導入單詞]之新增ᵣ [添加記錄]
/// </summary>
	public async Task<Func<
		IUserCtx
		,DtoAddWords
		,CT
		,Task<nil>
	>> FnMergeDtoAddWordsInToDb(
		IDbFnCtx Ctx
		,CT Ct
	){
		var InsertJnWords = await DaoWord.FnInsertJnWords(Ctx, Ct);
		var InsertPoKvs = await DaoWord.FnInsertPoKvs(Ctx, Ct);
		var InsertPoLearns = await DaoWord.FnInsertPoLearns(Ctx, Ct);
		var UpdUpd = await RepoPoWord.FnUpd_UpdatedAt(Ctx,Ct);

		var Fn = async(
			IUserCtx UserCtx
			,DtoAddWords DtoAddWords
			,CT Ct
		)=>{
			await using var NeoWords = new BatchListAsy<JnWord, nil>(InsertJnWords);
			await using var NeoProps = new BatchListAsy<PoWordProp, nil>(InsertPoKvs);
			await using var NeoLearns = new BatchListAsy<PoWordLearn, nil>(InsertPoLearns);



			foreach(var OneNonExisting in DtoAddWords.NeoWords){
				OneNonExisting.StoredAt = Tempus.Now();
				//var NeoPoLearns = MkPoLearns(OneNonExisting.Props, OneNonExisting.Id);
				await NeoWords.Add(OneNonExisting, Ct);
				//await NeoLearns.AddMany(NeoPoLearns, null, Ct);
			}

			// 有變動之諸新詞
			foreach(var UpdatedWord in DtoAddWords.UpdatedWords){
				if(UpdatedWord.DiffedWord == null){
					continue;
				}
				//若NewProps則有變動、學習記錄添'add'
				//var NeoPoLearns = MkPoLearns(UpdatedWord.DiffedWord.Props, UpdatedWord.WordInDb.Id);
				//await NeoLearns.AddMany(NeoPoLearns, null, Ct);
				UpdatedWord.DiffedWord.Props = UpdatedWord.DiffedWord.Props.Select(x=>{
					x.WordId = UpdatedWord.WordInDb.Id;
					return x;
				}).ToList();
				UpdatedWord.DiffedWord.Learns = UpdatedWord.DiffedWord.Learns.Select(x=>{
					x.WordId = UpdatedWord.WordInDb.Id;
					return x;
				}).ToList();
				await NeoProps.AddMany(UpdatedWord.DiffedWord.Props, null, Ct);
				await NeoLearns.AddMany(UpdatedWord.DiffedWord.Learns, null, Ct);
				await UpdUpd(UpdatedWord.WordInDb.Id, Ct);
			}

			await NeoWords.End(Ct);
			await NeoProps.End(Ct);
			await NeoLearns.End(Ct);

			return NIL;
		};
		return Fn;
	}

	public async Task<Func<
		IUserCtx
		,DtoAddWords
		,CT
		,Task<nil>
	>> FnAddOrUpdWordsFromTxtByDto(
		IDbFnCtx Ctx
		,CT Ct
	){
		var MergeWordsByDto = await FnMergeDtoAddWordsInToDb(Ctx, Ct);
		var InsertPoLearns = await DaoWord.FnInsertPoLearns(Ctx, Ct);
		var R = async(
			IUserCtx UserCtx
			,DtoAddWords DtoAddWords
			,CT Ct
		)=>{
			await using var NeoLearns = new BatchListAsy<PoWordLearn, nil>(InsertPoLearns);
			//按新ʹProps 決 添加記錄
			foreach(var OneNonExisting in DtoAddWords.NeoWords){
				var NeoPoLearns = MkPoLearns(OneNonExisting.Props, OneNonExisting.Id);
				await NeoLearns.AddMany(NeoPoLearns, null, Ct);
			}
			foreach(var UpdatedWord in DtoAddWords.UpdatedWords){
				if(UpdatedWord.DiffedWord == null){
					continue;
				}
				var NeoPoLearns = MkPoLearns(UpdatedWord.DiffedWord.Props, UpdatedWord.WordInDb.Id);
				await NeoLearns.AddMany(NeoPoLearns, null, Ct);
			}
			await MergeWordsByDto(UserCtx, DtoAddWords, Ct);
			return NIL;
		};
		return R;
	}



	/// <summary>
	/// 專用于添詞芝從文本詞表
	/// </summary>
	/// <param name="Ctx"></param>
	/// <param name="Ct"></param>
	/// <returns></returns>
	public async Task<Func<
		IUserCtx
		,IEnumerable<JnWord>
		,CT
		,Task<DtoAddWords>
	>> FnAddOrUpdWordsFromTxt(
		IDbFnCtx Ctx
		,CT Ct
	){
		var ClassifyWordsToAdd = await FnClassifyWordsToAdd(Ctx, Ct);
		var AddOrUpdateWordsByDto = await FnAddOrUpdWordsFromTxtByDto(Ctx,Ct);
		var Fn = async(
			IUserCtx UserCtx
			,IEnumerable<JnWord> JnWords
			,CT Ct
		)=>{
			var DtoAddWords = await ClassifyWordsToAdd(UserCtx, JnWords, Ct);
			await AddOrUpdateWordsByDto(UserCtx, DtoAddWords, Ct);
			return DtoAddWords;
		};
		return Fn;
	}


	public async Task<Func<
		IUserCtx
		,IEnumerable<JnWord>
		,CT
		,Task<DtoAddWords>
	>> FnMergeWordsIntoDb(
		IDbFnCtx Ctx
		,CT Ct
	){
		var ClassifyWordsToAdd = await FnClassifyWordsToAdd(Ctx, Ct);
		var AddOrUpdateWordsByDto = await FnMergeDtoAddWordsInToDb(Ctx,Ct);
		var Fn = async(
			IUserCtx UserCtx
			,IEnumerable<JnWord> JnWords
			,CT Ct
		)=>{
			var DtoAddWords = await ClassifyWordsToAdd(UserCtx, JnWords, Ct);
			await AddOrUpdateWordsByDto(UserCtx, DtoAddWords, Ct);
			return DtoAddWords;
		};
		return Fn;
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
		return await DaoWord.FnPageJnWords(Ctx,Ct);
	}


	public async Task<Func<
		IUserCtx
		,IdWord
		,CT
		,Task<JnWord?>
	>> FnGetJnWordByIdEtCheckOwner(
		IDbFnCtx Ctx
		,CT Ct
	){
		var SelectJnWordById = await DaoWord.FnSlctJnWordById(Ctx, Ct);
		var Fn = async(
			IUserCtx UserCtx
			,IdWord IdWord
			,CT Ct
		)=>{
			var JWord = await SelectJnWordById(IdWord, Ct);
			if(JWord == null){
				return JWord;
			}

			if(JWord.Owner != UserCtx.UserId){
				throw EErr.WordOwnerNotMatch().ToErrBase();
			}
			return JWord;
		};
		return Fn;
	}

	/// <summary>
	/// 潙已有ʹ詞 增 新ʹ學ˡ錄
	/// </summary>
	/// <param name="Ctx"></param>
	/// <param name="Ct"></param>
	/// <returns></returns>
	public async Task<Func<
		IUserCtx
		,IEnumerable<WordId_PoLearns>
		,CT
		,Task<nil>
	>> FnAddWordId_PoLearnss(
		IDbFnCtx Ctx
		,CT Ct
	){
		var CheckWordOwner = await FnGetJnWordByIdEtCheckOwner(Ctx, Ct);
		var InsertPoLearns = await DaoWord.FnInsertPoLearns(Ctx, Ct);
		var UpdUpd = await RepoPoWord.FnUpd_UpdatedAt(Ctx,Ct);
		var Fn = async(
			IUserCtx UserCtx
			,IEnumerable<WordId_PoLearns> ListOfWordId_PoLearns
			,CT Ct
		)=>{
			foreach(var WordId_PoLearns in ListOfWordId_PoLearns){
				var IdWord = WordId_PoLearns.WordId;
				await CheckWordOwner(UserCtx, IdWord, Ct);
				var PoLearns = WordId_PoLearns.PoLearns.Select(x=>{
					x.WordId = IdWord;
					return x;
				});
				await InsertPoLearns(PoLearns, Ct);
				await UpdUpd(IdWord, Ct);
			}
			return NIL;
		};
		return Fn;
	}

	// [Obsolete("宜用軟刪")]
	// public async Task<Func<
	// 	IUserCtx
	// 	,IEnumerable<IdWord>
	// 	,CT
	// 	,Task<nil>
	// >> FnDeleteJnWordsByIds(
	// 	IDbFnCtx Ctx, CT Ct
	// ){
	// 	var CheckOwner = await FnCheckWordOwnerOrThrow(Ctx, Ct);

	// 	var DelPoWordById = await RepoPoWord.FnDeleteManyByKeys<IdWord>(
	// 		Ctx, nameof(PoWord.Id), 1000, Ct
	// 	);
	// 	var DelPoKvByWordIds = await RepoKv.FnDeleteManyByKeys<IdWord>(
	// 		Ctx, nameof(PoWordProp.WordId), 1000, Ct
	// 	);
	// 	var DelPoLearnByWordIds = await RepoLearn.FnDeleteManyByKeys<IdWord>(
	// 		Ctx, nameof(PoWordLearn.WordId), 1000, Ct
	// 	);
	// 	var Fn = async(
	// 		IUserCtx UserCtx
	// 		,IEnumerable<IdWord> Ids
	// 		,CT Ct
	// 	)=>{
	// 		Ids = Ids.Select(Id=>{
	// 			_ = CheckOwner(UserCtx, Id, Ct).Result;
	// 			return Id;
	// 		});
	// 		await DelPoWordById(Ids, Ct);
	// 		await DelPoKvByWordIds(Ids, Ct);
	// 		await DelPoLearnByWordIds(Ids, Ct);
	// 		return NIL;
	// 	};
	// 	return Fn;
	// }

	public async Task<Func<
		IUserCtx
		,IEnumerable<IdWord>
		,CT
		,Task<nil>
	>> FnSoftDelJnWordsByIds(
		IDbFnCtx Ctx ,CT Ct
	){
		var CheckOwner = await FnGetJnWordByIdEtCheckOwner(Ctx, Ct);
		var SoftDelPoWordById = await RepoPoWord.FnSoftDelManyByKeys<IdWord>(Ctx, nameof(PoWord.Id), 1000, Ct);
		var DelPoKvByWordIds = await RepoKv.FnSoftDelManyByKeys<IdWord>(Ctx, nameof(PoWordProp.WordId), 1000, Ct);
		var DelPoLearnByWordIds = await RepoLearn.FnSoftDelManyByKeys<IdWord>(Ctx, nameof(PoWordLearn.WordId), 1000, Ct);
		var Fn = async(
			IUserCtx UserCtx
			,IEnumerable<IdWord> Ids
			,CT Ct
		)=>{
			Ids = Ids.Select(Id=>{
				_ = CheckOwner(UserCtx, Id, Ct).Result;
				return Id;
			});
			await SoftDelPoWordById(Ids, Ct);
			await DelPoKvByWordIds(Ids, Ct);
			await DelPoLearnByWordIds(Ids, Ct);
			return NIL;
		};
		return Fn;
	}

	/// <summary>
	/// 備份同步㕥合併同ʹ詞旹、當按詞頭洏非id㕥判兩詞是否潙同一詞、緣縱潙同ʹ詞、本地ʹ庫ʸ與遠端ᐪʹid恐不一
	/// //TODO 一致ˢid、以CreatedAt最早者潙準
	/// </summary>
	/// <param name="Ctx"></param>
	/// <param name="Ct"></param>
	/// <returns></returns>
	public async Task<Func<
		IUserCtx
		,JnWord
		,CT,Task<nil>
	>> FnSyncMergeIn(IDbFnCtx Ctx, CT Ct){
		var CheckOwner = await FnGetJnWordByIdEtCheckOwner(Ctx, Ct);
		var SlctWord = await DaoWord.FnSlctIdByOwnerHeadLang(Ctx, Ct);
		var Fn = async(IUserCtx UserCtx, JnWord NeoWords, CT Ct)=>{
			var Existing = await SlctWord(UserCtx, NeoWords.Head, NeoWords.Lang, Ct);
			if(Existing == null){

			}else{

			}
			return NIL;
		};
		return Fn;
	}


/// <summary>
/// IIter<str>: 每次返一行 JnWord Json。文件ʹ json格式潙按換行符分隔之 獨立ₐ JnWord json、非JnWord列表
/// 胡不用 JnWord[]? 緣用列表則其元素有「,」間隔、不易流式讀
/// </summary>
/// <returns></returns>
	public async Task<Func<
		IUserCtx
		,IAsyncEnumerable<str>
		,CT
		,Task<nil>
	>> FnAddWordsByJsonLineIter(
		IDbFnCtx Ctx
		,CT Ct
	){
		var FnAddWords = await FnMergeWordsIntoDb(Ctx, Ct);
		var R = async (
			IUserCtx User
			,IAsyncEnumerable<str> JsonLineIter
			,CT Ct
		)=>{
			await using var Bl = new BatchListAsy<JnWord, nil>(async (words, Ct)=>{
				await FnAddWords(User, words, Ct);
				return NIL;
			});
			await foreach(var Line in JsonLineIter){
				var JnWord = JsonSerializer.Parse<JnWord>(Line);
				await Bl.Add(JnWord, Ct);
			}
			await Bl.End(Ct);
			return NIL;
		};
		return R;
	}

	// public async Task<Func<
	// 	IUserCtx
	// 	,IEnumerable<IdWord>
	// 	,CT
	// 	,Task<nil>
	// >> FnSync(){

	// }


	// public async Task<Func<
	// 	IUserCtx
	// 	,CT
	// 	,Task<nil>
	// >> FnSyncDb(

	// ){

	// 	var Fn = async(

	// 	)=>{
	// 		return NIL;
	// 	};
	// 	return Fn;
	// }

	public async Task<Func<
		IUserCtx
		,IPageQry
		,ReqSearchWord
		,CT
		,Task<IPage<JnWord>>
	>> FnSearchWord(IDbFnCtx Ctx, CT Ct){
		var PageSearchIdsByPrefix = await DaoWord.FnPageSearchWordIdsByHeadPrefix(Ctx, Ct);
		var CheckWordOwnerOrThrow = await FnGetJnWordByIdEtCheckOwner(Ctx, Ct);
		return async (User, PageQry, Req, Ct)=>{
			var IdPage = await PageSearchIdsByPrefix(User, PageQry, Req, Ct);
			List<JnWord> Words = [];
			try{
				var WordId = IdWord.FromLow64Base(Req.RawStr);
				var Word = await CheckWordOwnerOrThrow(User, WordId, Ct);
				if(Word is not null){
					Words.Add(Word);
				}
			}catch (System.Exception){
				//TODO 判斷異常類型
			}
			if(IdPage.DataAsyE is not null){
				await foreach(var IdWord in IdPage.DataAsyE){
					var Word = await CheckWordOwnerOrThrow(User, IdWord, Ct);//不應再拋異常
					if(Word is not null){
						Words.Add(Word);
					}
				}
			}
			var R = Page.Mk(PageQry, Words);
			return R;
		};
	}


/// <summary>
	/// 返(詞頭,語言)對應之id
	/// 庫中無新改ʹ(詞頭,語言)則返源ʹ詞ʹid
	/// 若有新改ʹ(詞頭,語言) 即把源詞合併入目標詞後 返舊詞ʹid
	/// 傳入ʹIdWordˋ在庫中尋不見旹返null
	/// </summary>
	/// <exception cref="FatalLogicErr"></exception>
	public async Task<Func<
		IUserCtx
		,IdWord
		,str//head
		,str//lang
		,CT
		,Task<IdWord?> //詞頭對應之id
	>> FnUpdWordHeadLang(IDbFnCtx Ctx, CT Ct){
		var GetJnWordByIdEtCheckOwner = await FnGetJnWordByIdEtCheckOwner(Ctx, Ct);
		var UpdWordHeadLang = await DaoWord.FnUpdPoWordHeadLang(Ctx, Ct);
		var SlctIdByOwnerHeadLang = await DaoWord.FnSlctIdByOwnerHeadLang(Ctx, Ct);
		var MergeWordsIntoDb = await FnMergeWordsIntoDb(Ctx, Ct);
		var SoftDelJnWordById = await FnSoftDelJnWordsByIds(Ctx,Ct);
		var UpdPropForeignWordIdById = await RepoKv.FnUpdOneColById(Ctx, nameof(PoWordProp.WordId), Ct);
		var UpdLearnForeignWordIdById = await RepoLearn.FnUpdOneColById(Ctx, nameof(PoWordLearn.WordId), Ct);

		return async (User, IdWord, Head, Lang, Ct)=>{
			var WordWithId = await GetJnWordByIdEtCheckOwner(User, IdWord, Ct);
			if(WordWithId is null){
				return null;
			}
			//var Lang = SrcWord.Lang;
			var TargetId = await SlctIdByOwnerHeadLang(User, Head, Lang, Ct);
			if(TargetId is null){
				await UpdWordHeadLang(User, IdWord, Head, Lang, Ct);
				return IdWord;
			}
			var WordWithTextLang = await GetJnWordByIdEtCheckOwner(User, TargetId.Value, Ct) ?? throw new FatalLogicErr("Existing is null");
			var RawWordId = DaoWord.IdUpperToRaw<PoWord>(TargetId);
			foreach(var prop in WordWithId.Props){
				await UpdPropForeignWordIdById(prop.Id, RawWordId, Ct);
			}
			foreach(var learn in WordWithId.Learns){
				await UpdLearnForeignWordIdById(learn.Id, RawWordId , Ct);
			}
			await SoftDelJnWordById(User, [IdWord], Ct);
			return TargetId;
		};
	}

	// /// <summary>
	// /// 返(詞頭,語言)對應之id
	// /// 庫中無新改ʹ(詞頭,語言)則返源ʹ詞ʹid
	// /// 若有新改ʹ(詞頭,語言) 即把源詞合併入目標詞後 返舊詞ʹid
	// /// 傳入ʹIdWordˋ在庫中尋不見旹返null
	// /// </summary>
	// /// <exception cref="FatalLogicErr"></exception>
	// public async Task<Func<
	// 	IUserCtx
	// 	,IdWord
	// 	,str//head
	// 	,str//lang
	// 	,CT
	// 	,Task<IdWord?> //詞頭對應之id
	// >> FnUpdWordHeadLang(IDbFnCtx Ctx, CT Ct){
	// 	var GetJnWordByIdEtCheckOwner = await FnGetJnWordByIdEtCheckOwner(Ctx, Ct);
	// 	var UpdWordHeadLang = await DaoWord.FnUpdPoWordHeadLang(Ctx, Ct);
	// 	var SlctIdByOwnerHeadLang = await DaoWord.FnSlctIdByOwnerHeadLang(Ctx, Ct);
	// 	var MergeWordsIntoDb = await FnMergeWordsIntoDb(Ctx, Ct);
	// 	var SoftDelJnWordById = await FnSoftDelJnWordsByIds(Ctx,Ct);

	// 	return async (User, IdWord, Head, Lang, Ct)=>{
	// 		var WordWithId = await GetJnWordByIdEtCheckOwner(User, IdWord, Ct);
	// 		if(WordWithId is null){
	// 			return null;
	// 		}
	// 		//var Lang = SrcWord.Lang;
	// 		var TargetId = await SlctIdByOwnerHeadLang(User, Head, Lang, Ct);
	// 		if(TargetId is null){
	// 			await UpdWordHeadLang(User, IdWord, Head, Lang, Ct);
	// 			return IdWord;
	// 		}
	// 		var WordWithTextLang = await GetJnWordByIdEtCheckOwner(User, TargetId.Value, Ct) ?? throw new FatalLogicErr("Existing is null");
	// 		//先統一(詞頭, 語言) 㕥防diff報錯
	// 		WordWithId.Head = Head;
	// 		WordWithId.Lang = Lang;
	// 		var DiffedWord = WordWithId.Diff(WordWithTextLang);
	// 		if(DiffedWord is null){//無差
	// 			await SoftDelJnWordById(User, [IdWord], Ct);
	// 			return TargetId;
	// 		}
	// 		//先刪舊詞汶合新詞ʹ盈部、免Id衝突
	// 		await SoftDelJnWordById(User, [IdWord], Ct);
	// 		await MergeWordsIntoDb(User, [DiffedWord], Ct);
	// 		return TargetId;
	// 	};
	// }


	/// <summary>
	/// 更新JnWord。以新傳入之JnWord潙基準、缺者補 盈者刪
	/// 以id潙基準
	/// </summary>
	/// <param name="Ctx"></param>
	/// <param name="Ct"></param>
	/// <returns></returns>
	/// <exception cref="FatalLogicErr"></exception>
	public async Task<Func<
		IUserCtx
		,JnWord
		,CT
		,Task<nil>
	>> FnUpdJnWord(IDbFnCtx Ctx, CT Ct){
		var GetJnWordByIdEtCheckOwner = await FnGetJnWordByIdEtCheckOwner(Ctx, Ct);
		var UpdWordHeadLang = await FnUpdWordHeadLang(Ctx, Ct);
		var MergeWordsIntoDb = await FnMergeWordsIntoDb(Ctx, Ct);
		var SofeDelPropsByIds = await RepoKv.FnSoftDelManyByKeys<IdWordProp>(Ctx, nameof(PoWordProp.Id), 1000, Ct);
		var SofeDelLearnByIds = await RepoLearn.FnSoftDelManyByKeys<IdWordLearn>(Ctx, nameof(PoWordLearn.Id), 1000, Ct);
//var UpdUpd = await RepoPoWord.FnUpd_UpdatedAt(Ctx,Ct);
		return async(User, JnWord, Ct)=>{
			var OldWord = await GetJnWordByIdEtCheckOwner(User, JnWord.Id, Ct);
			if(OldWord is null){//JnWord潙新詞(其Id不存于數據庫)
				//清洗ID、不 用ᵣ用戶ʃ輸
				JnWord.Id = new IdWord();
				foreach(var Prop in JnWord.Props){
					Prop.Id = new IdWordProp();
				}
				foreach(var Learn in JnWord.Learns){
					Learn.Id = new IdWordLearn();
				}
				JnWord.EnsureForeignId();

				await MergeWordsIntoDb(User, [JnWord], Ct);
				return NIL;
			}else{//JnWord非新詞(其Id己存于數據庫)
				if(JnWord.Owner != User.UserId){
					throw EErr.WordOwnerNotMatch().ToErrBase();
				}

				if(JnWord.Head != OldWord.Head || JnWord.Lang != OldWord.Lang){
					var amended = await UpdWordHeadLang(User, JnWord.Id, JnWord.Head, JnWord.Lang, Ct)?? throw new FatalLogicErr("Existing is null");
					// if(amended != OldWord.Id){
					// 	throw new FatalLogicErr("amended != OldWord.Id");
					// }
					JnWord.Id = amended;
				}

				OldWord.Head = JnWord.Head;//先改老詞ʹ (詞頭,語言)、否則後ʹdiff旹報錯曰非同一詞
				OldWord.Lang = JnWord.Lang;

				var NeoDiffOld = JnWord.Diff(OldWord);//JnWord比OldWord多出之內容
				var OldDiffNeo = OldWord.Diff(JnWord);//OldWord比JnWord多出之內容
				if(NeoDiffOld is not null){
					await MergeWordsIntoDb(User, [NeoDiffOld], Ct);
				}
				if(OldDiffNeo is null){
					return NIL;
				}
				await using var DelProps = new BatchListAsy<IdWordProp, nil>(async(ids, Ct)=>{
					await SofeDelPropsByIds(ids, Ct);
					return NIL;
				});
				await using var DelLearns = new BatchListAsy<IdWordLearn, nil>(async(ids, Ct)=>{
					return await SofeDelLearnByIds(ids, Ct);
				});
				foreach(var Prop in OldDiffNeo.Props){
					await DelProps.Add(Prop.Id, Ct);
				}
				foreach(var Learn in OldDiffNeo.Learns){
					await DelLearns.Add(Learn.Id, Ct);
				}
			}
			return NIL;
		};
	}

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
	#endregion #region API

}
