namespace Ngaq.Local.Word.Svc;
using Ngaq.Core.Infra.Core;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Model.Word.Dto;
using Ngaq.Core.Service.Word;
using Ngaq.Core.Tools.Io;
using Ngaq.Core.Infra.Errors;
using Tsinswreng.CsTools;
using Ngaq.Core.Model.Word.Req;
using Tsinswreng.CsSqlHelper;
using Ngaq.Core.Word.Svc;
using Tsinswreng.CsCore;
using Tsinswreng.CsPage;
using Ngaq.Core.Infra;
using Ngaq.Core.Models;
using Ngaq.Local.Word.Dao;
using Ngaq.Local.Db.TswG;
using Ngaq.Core.Tools.Json;
using Ngaq.Core.Word.Models.Dto;
using Ngaq.Core.Domains.User.UserCtx;
using Ngaq.Core.Domains.Word.Models.Po.Kv;
using Ngaq.Core.Domains.Word.Models.Learn_;
using Ngaq.Core.Domains.Word.Models;
using Ngaq.Core.Domains.Word.Models.Po.Word;
using Ngaq.Core.Domains.Word.Models.Po.Learn;

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

	protected IList<PoWordLearn> MkPoLearnList(IEnumerable<PoWordProp> NeoProps, IdWord WordId){
		IList<PoWordLearn> R = new List<PoWordLearn>();
		foreach(var Prop in NeoProps){
			if(Prop.KStr == KeysProp.Inst.description){
				var U = new PoWordLearn();
				U.BizCreatedAt = Prop.BizCreatedAt;
				U.LearnResult = ELearn.Add;
				U.WordId = WordId;
				R.Add(U);
			}
		}
		return R;
	}

	protected IEnumerable<PoWordLearn> MkPoLearnEnume(IEnumerable<PoWordProp> NeoProps, IdWord WordId){
		foreach(var Prop in NeoProps){
			if(Prop.KStr == KeysProp.Inst.description){
				var U = new PoWordLearn();
				U.BizCreatedAt = Prop.BizCreatedAt;
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
		var UpdUpd = await DaoWord.FnTriggerOnRootAfterUpd(Ctx, Ct);

		var Fn = async(
			IUserCtx UserCtx
			,DtoAddWords DtoAddWords
			,CT Ct
		)=>{
			await using var NeoWords = new BatchListAsy<JnWord, nil>(InsertJnWords);
			await using var NeoProps = new BatchListAsy<PoWordProp, nil>(async(kvs, Ct)=>{
				return await InsertPoKvs(null, kvs, Ct);
			});
			await using var NeoLearns = new BatchListAsy<PoWordLearn, nil>(async(learns, Ct)=>{
				return await InsertPoLearns(null, learns, Ct);
			});


			foreach(var OneNonExisting in DtoAddWords.NeoWords){
				OneNonExisting.StoredAt = Tempus.Now();
				//var NeoPoLearns = MkPoLearns(OneNonExisting.Props, OneNonExisting.Id);
				await NeoWords.Add(OneNonExisting, Ct);
				//await NeoLearns.AddMany(NeoPoLearns, null, Ct);
			}

			// 有變動之諸新詞
			foreach(var UpdatedWord in DtoAddWords.UpdatedWords){
				var DiffedWord = UpdatedWord.DiffedWord;
				if(DiffedWord == null){
					continue;
				}
				if(DiffedWord.Props.Count == 0 && DiffedWord.Learns.Count == 0){
					continue;
				}
				//若NewProps則有變動、學習記錄添'add'
				DiffedWord.Props = DiffedWord.Props.Select(x=>{
					x.WordId = UpdatedWord.WordInDb.Id;
					return x;
				}).ToList();
				DiffedWord.Learns = DiffedWord.Learns.Select(x=>{
					x.WordId = UpdatedWord.WordInDb.Id;
					return x;
				}).ToList();
				var WordId = DiffedWord.Id;
				await NeoProps.AddMany(DiffedWord.Props, null, Ct);
				await NeoLearns.AddMany(DiffedWord.Learns, null, Ct);
				await UpdUpd(WordId, Ct);
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
		var UpdUpd = await DaoWord.FnTriggerOnRootAfterUpd(Ctx, Ct);
		var R = async(
			IUserCtx UserCtx
			,DtoAddWords DtoAddWords
			,CT Ct
		)=>{
			await using var NeoLearns = new BatchListAsy<PoWordLearn, nil>(async (learns, Ct)=>{
				return await InsertPoLearns(null, learns, Ct);
			});

			//按新ʹProps 決 添加記錄
			foreach(var OneNonExisting in DtoAddWords.NeoWords){
				var NeoPoLearns = MkPoLearnList(OneNonExisting.Props, OneNonExisting.Id);
				await NeoLearns.AddMany(NeoPoLearns, null, Ct);
			}
			foreach(var UpdatedWord in DtoAddWords.UpdatedWords){
				if(UpdatedWord.DiffedWord == null){
					continue;
				}
				var NeoPoLearns = MkPoLearnList(UpdatedWord.DiffedWord.Props, UpdatedWord.WordInDb.Id);
				if(NeoPoLearns.Count > 0){
					await NeoLearns.AddMany(NeoPoLearns, null, Ct);
					await UpdUpd(UpdatedWord.WordInDb.Id, Ct);
				};
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
			JnWords = JnWords.Select(x=>{
				x.Owner = UserCtx.UserId;
				return x;
			});
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
		//var UpdUpd = await DaoWord.FnTriggerOnRootAfterUpd(Ctx,Ct);
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
				await InsertPoLearns(IdWord, PoLearns, Ct);
				//await UpdUpd(IdWord, Ct);
			}
			return NIL;
		};
		return Fn;
	}


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
		var UpdUpd = await DaoWord.FnTriggerOnRootAfterUpd(Ctx,Ct);
		var Fn = async(
			IUserCtx UserCtx
			,IEnumerable<IdWord> Ids
			,CT Ct
		)=>{
			// 多次遍歷IEnumerable<T> 恐不安全
			Ids = Ids.Select(Id=>{
				_ = CheckOwner(UserCtx, Id, Ct).Result;
				return Id;
			}).ToListTryNoCopy();
			foreach(var Id in Ids){
				await UpdUpd(Id, Ct);
			}
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
		//var UpdUpd = await DaoWord.FnTriggerOnRootAfterUpd(Ctx,Ct);

		return async (User, IdWord, Head, Lang, Ct)=>{
			var WordWithId = await GetJnWordByIdEtCheckOwner(User, IdWord, Ct);
			if(WordWithId is null){
				return null;
			}
			//var Lang = SrcWord.Lang;
			var TargetId = await SlctIdByOwnerHeadLang(User, Head, Lang, Ct);
			if(TargetId is null){
				await UpdWordHeadLang(User, IdWord, Head, Lang, Ct);
				//await UpdUpd(IdWord, Ct);
				return IdWord;
			}
			var WordWithTextLang = await GetJnWordByIdEtCheckOwner(User, TargetId.Value, Ct) ?? throw new FatalLogicErr("Existing is null");
			var RawWordId = DaoWord.IdUpperToRaw<PoWord>(TargetId);
			//await UpdUpd(TargetId.Value, Ct);
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


	public async Task<Func<
		IUserCtx
		,IdWordProp
		,CT
		,Task<nil>
	>> FnDelPropById(IDbFnCtx Ctx, CT CT){
		throw new NotImplementedException();
	}


	/// <summary>
	/// 更新JnWord。以新傳入之JnWord潙基準、缺者補 盈者刪
	/// 以id潙基準
	/// </summary>
	/// <param name="Ctx"></param>
	/// <param name="Ct"></param>
	/// <returns></returns>
	/// <exception cref="FatalLogicErr"></exception>
	/// //TODO Id或時間爲空時先自動填充
	/// TODO 返回實ʹ操作 如 無變, 有增 等
	/// 多個Prop芝厥CreatedAt同旹 減其一汶保存、不效 ʃ被減者未被刪 緣時ˋ同旹diff不出

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
		//var UpdUpd = await DaoWord.FnTriggerOnRootAfterUpd(Ctx, Ct);
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
				//(詞頭, 語言)ˉ標識 有變
				if(JnWord.Head != OldWord.Head || JnWord.Lang != OldWord.Lang){
					var amended = await UpdWordHeadLang(User, JnWord.Id, JnWord.Head, JnWord.Lang, Ct)?? throw new FatalLogicErr("Existing is null");
					// if(amended != OldWord.Id){
					// 	throw new FatalLogicErr("amended != OldWord.Id");
					// }
					JnWord.Id = amended;
				}

				OldWord.Head = JnWord.Head;//先改老詞ʹ (詞頭,語言)、否則後ʹdiff旹報錯曰非同一詞
				OldWord.Lang = JnWord.Lang;

				foreach(var Prop in JnWord.Props){
					if(Prop.Id.Value == 0){//新增條目 則設ID潙0
						Prop.Id = new IdWordProp();
						Prop.BizCreatedAt = new Tempus();
					}
				}
				foreach(var Learn in JnWord.Learns){
					if(Learn.Id.Value == 0){
						Learn.Id = new IdWordLearn();
						Learn.BizCreatedAt = new Tempus();
					}
				}
				JnWord.EnsureForeignId();

				var NeoDiffOld = JnWord.DiffByTime(OldWord);//JnWord比OldWord多出之內容
				var OldDiffNeo = OldWord.DiffByTime(JnWord);//OldWord比JnWord多出之內容
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


}
