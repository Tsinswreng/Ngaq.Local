namespace Ngaq.Local.Domains.Word.Svc;
using Ngaq.Core.Infra.Core;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Model.Po.Word;

using Ngaq.Core.Infra.Errors;
using Tsinswreng.CsTools;
using Ngaq.Core.Model.Word.Req;
using Tsinswreng.CsSqlHelper;
using Ngaq.Core.Word.Svc;

using Ngaq.Core.Infra;

using Ngaq.Local.Word.Dao;
using Ngaq.Local.Db.TswG;
using Ngaq.Core.Tools.Json;

using Ngaq.Core.Shared.User.UserCtx;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Shared.Word.Models.Learn_;
using Ngaq.Core.Shared.Word.Models;
using Ngaq.Core.Shared.Word.Models.Po.Word;
using Ngaq.Core.Shared.Word.Models.Po.Learn;
using Ngaq.Core.Shared.Word.Models.Dto;
using Ngaq.Core.Shared.Word.Svc;
using Ngaq.Core.Tools;
using System.Collections;
using Ngaq.Core.Shared.Base.Models.Po;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

public partial class SvcWord(
	ISvcParseWordList SvcParseWordList
	,ITxnRunner TxnRunner
	,DaoSqlWord DaoWord
	,I_GetTxnAsy TxnGetter
	,IAppRepo<PoWord, IdWord> RepoPoWord
	,IAppRepo<PoWordProp, IdWordProp> RepoProp
	,IAppRepo<PoWordLearn, IdWordLearn> RepoLearn
	,TxnWrapper<DbFnCtx> TxnWrapper
	,IJsonSerializer JsonSerializer
	,ILogger Logger
)
	: ISvcWord
{

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
	/// 若JnWord已被軟刪則軟刪、否則添。用于匯入遠端ʸ來ʹ詞
	/// </summary>
	/// <param name="Ctx"></param>
	/// <param name="Ct"></param>
	/// <returns></returns>
	// public async Task<Func<
	// 	IUserCtx, IEnumerable<IJnWord>, CT, Task<nil>
	// >> FnInsertOrDelWords(IDbFnCtx Ctx, CT Ct){
	// 	var InsertJnWords = await DaoWord.FnInsertJnWords(Ctx, Ct);
	// 	var SoftDelMany = await FnSoftDelJnWordsByIds(Ctx, Ct);
	// 	return async(User, Words, Ct)=>{
	// 		var IdsToDel = new List<IdWord>();
	// 		var NonDeleted = new List<IJnWord>();
	// 		foreach(var w in Words){
	// 			if(w.Word.IsDeleted()){
	// 				IdsToDel.Add(w.Word.Id);
	// 			}else{
	// 				w.Word.Owner = User.UserId;
	// 				NonDeleted.Add(w);
	// 			}
	// 		}
	// 		await SoftDelMany(User, IdsToDel, Ct);
	// 		await InsertJnWords(NonDeleted, Ct);
	// 		return NIL;
	// 	};
	// }


/// <summary>
/// 把DtoAddWords 寫入數據庫。不類[從文本生詞表導入單詞]之新增ᵣ [添加記錄]
/// </summary>
	public async Task<Func<
		IUserCtx
		,DtoAddWordsOld
		,CT
		,Task<nil>
	>> FnMergeDtoAddWordsInToDbOld(
		IDbFnCtx Ctx
		,CT Ct
	){
		var InsertJnWords = await DaoWord.FnInsertJnWords(Ctx, Ct);
		var InsertPoKvs = await DaoWord.FnInsertPoKvs(Ctx, Ct);
		var InsertPoLearns = await DaoWord.FnInsertPoLearns(Ctx, Ct);
		var UpdUpd = await DaoWord.FnTriggerOnRootAfterUpd(Ctx, Ct);

		return async(UserCtx, DtoAddWords, Ct)=>{
			await using var NeoWords = new BatchCollector<IJnWord, nil>(InsertJnWords);
			await using var NeoProps = new BatchCollector<PoWordProp, nil>(async(kvs, Ct)=>{
				return await InsertPoKvs(null, kvs, Ct);
			});
			await using var NeoLearns = new BatchCollector<PoWordLearn, nil>(async(learns, Ct)=>{
				return await InsertPoLearns(null, learns, Ct);
			});


			foreach(var OneNonExisting_ in DtoAddWords.NeoWords){
				var OneNonExisting = OneNonExisting_.AsOrToJnWord();
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
					x.WordId = UpdatedWord.WordInDb.Id_();
					return x;
				}).ToList();
				var WordId = DiffedWord.Id_();
				await NeoProps.AddRange(DiffedWord.Props, null, Ct);
				await NeoLearns.AddRange(DiffedWord.Learns, null, Ct);
				await UpdUpd(WordId, Ct);
			}

			await NeoWords.End(Ct);
			await NeoProps.End(Ct);
			await NeoLearns.End(Ct);

			return NIL;
		};
	}



	/// <summary>
	/// 把DtoAddWords 寫入數據庫。不類[從文本生詞表導入單詞]之新增ᵣ [添加記錄]
	/// </summary>
	[Obsolete("有蠹 實非sync 只是Merge")]
	public async Task<Func<
		IUserCtx
		,DtoSyncWordsOld2
		,CT
		,Task<nil>
	>> FnSyncFromDtoOld(
		IDbFnCtx Ctx, CfgMerge Cfg
		,CT Ct
	){
		var InsertJnWords = await DaoWord.FnInsertJnWords(Ctx, Ct);
		var InsertPoKvs = await DaoWord.FnInsertPoKvs(Ctx, Ct);
		var InsertPoLearns = await DaoWord.FnInsertPoLearns(Ctx, Ct);
		var UpdUpd = await DaoWord.FnTriggerOnRootAfterUpd(Ctx, Ct);

		var UpdProp = await RepoProp.FnUpdManyById(Ctx, null, Ct);
		var UpdLearn = await RepoLearn.FnUpdManyById(Ctx, null, Ct);
		var UpdPoWord = await RepoPoWord.FnUpdManyById(Ctx, null, Ct);

		return async(UserCtx, DtoAddWords, Ct)=>{
			var sw = Stopwatch.StartNew();
			await using var NeoWords = new BatchCollector<IJnWord, nil>(InsertJnWords);
			await using var NeoProps = new BatchCollector<PoWordProp, nil>(async(kvs, Ct)=>{
				return await InsertPoKvs(null, kvs, Ct);
			});
			await using var NeoLearns = new BatchCollector<PoWordLearn, nil>(async(learns, Ct)=>{
				return await InsertPoLearns(null, learns, Ct);
			});
			await using var UpdProps = new BatchCollector<PoWordProp, nil>(UpdProp);
			await using var UpdLearns = new BatchCollector<PoWordLearn, nil>(UpdLearn);
			await using var UpdPoWords = new BatchCollector<PoWord, nil>(UpdPoWord);

			foreach(var (i,OneNonExisting_) in DtoAddWords.NeoWords.Index()){
				var OneNonExisting = OneNonExisting_.AsOrToJnWord();
				OneNonExisting.StoredAt = Tempus.Now();
				//var NeoPoLearns = MkPoLearns(OneNonExisting.Props, OneNonExisting.Id);
				await NeoWords.Add(OneNonExisting, Ct);
				//await NeoLearns.AddMany(NeoPoLearns, null, Ct);
			}


			foreach(var UpdatedWord in DtoAddWords.UpdatedWords){
				{// 有變動之諸詞 處理 NeoPart

					if(UpdatedWord.NeoPart is not null
						&& !(UpdatedWord.NeoPart.Props.Count == 0 && UpdatedWord.NeoPart.Learns.Count == 0)
					){
						var NeoPart = UpdatedWord.NeoPart.AsOrToJnWord();
						NeoPart.Id = UpdatedWord.WordInDb.Id_();
						NeoPart.EnsureForeignId();

						var WordId = NeoPart.Id_();
						await NeoProps.AddRange(NeoPart.Props, null, Ct);
						await NeoLearns.AddRange(NeoPart.Learns, null, Ct);
					}
				}
				{// 有變動之諸詞 處理 ChangedPart

					if(UpdatedWord.ChangedPart is not null){
						var ChangedPart = UpdatedWord.ChangedPart.AsOrToJnWord();
						ChangedPart.Id = UpdatedWord.WordInDb.Id_();
						await UpdLearns.AddRange(ChangedPart.Learns, null, Ct);
						await UpdProps.AddRange(ChangedPart.Props, null, Ct);
						await UpdPoWords.Add(ChangedPart.Word, Ct);
					}
				}
				if(Cfg.AutoUpdBizUpdatedAt){
					await UpdUpd(UpdatedWord.WordInDb.Id_(), Ct);
				}
			}//~foreach(var UpdatedWord in DtoAddWords.UpdatedWords)
			sw.Stop();
			Logger.LogInformation($"SyncFromDto {sw.ElapsedMilliseconds}ms");
			return NIL;
		};
	}



	public async Task<Func<
		IUserCtx,DtoSyncWords
		,CT,Task<nil>
	>> FnSyncFromDto(
		IDbFnCtx Ctx, CfgMerge Cfg
		,CT Ct
	){
		var InsertJnWords = await DaoWord.FnInsertJnWords(Ctx, Ct);
		var InsertPoKvs = await DaoWord.FnInsertPoKvs(Ctx, Ct);
		var InsertPoLearns = await DaoWord.FnInsertPoLearns(Ctx, Ct);
		var UpdUpd = await DaoWord.FnTriggerOnRootAfterUpd(Ctx, Ct);

		var UpdProp = await RepoProp.FnUpdManyById(Ctx, null, Ct);
		var UpdLearn = await RepoLearn.FnUpdManyById(Ctx, null, Ct);
		var UpdPoWord = await RepoPoWord.FnUpdManyById(Ctx, null, Ct);

		return async(UserCtx, DtoAddWords, Ct)=>{
			var sw = Stopwatch.StartNew();
			await using var NeoWords = new BatchCollector<IJnWord, nil>(InsertJnWords);
			await using var NeoProps = new BatchCollector<PoWordProp, nil>(async(kvs, Ct)=>{
				return await InsertPoKvs(null, kvs, Ct);
			});
			await using var NeoLearns = new BatchCollector<PoWordLearn, nil>(async(learns, Ct)=>{
				return await InsertPoLearns(null, learns, Ct);
			});
			await using var UpdProps = new BatchCollector<PoWordProp, nil>(UpdProp);
			await using var UpdLearns = new BatchCollector<PoWordLearn, nil>(UpdLearn);
			await using var UpdPoWords = new BatchCollector<PoWord, nil>(UpdPoWord);

			foreach(var (i,OneNonExisting_) in DtoAddWords.NeoWords.Index()){
				var OneNonExisting = OneNonExisting_.AsOrToJnWord();
				OneNonExisting.StoredAt = Tempus.Now();
				//var NeoPoLearns = MkPoLearns(OneNonExisting.Props, OneNonExisting.Id);
				await NeoWords.Add(OneNonExisting, Ct);
				//await NeoLearns.AddMany(NeoPoLearns, null, Ct);
			}

			foreach(var UpdatedWord in DtoAddWords.UpdatedWords){
				var dtoSync = UpdatedWord.DtoSyncTwoWords;
				{// 有變動之諸詞 處理 NeoPart
					await NeoProps.AddRange(dtoSync.NeoOrChangedProps.NeoPart, null, Ct);
					await NeoLearns.AddRange(dtoSync.NeoOrChangedLearns.NeoPart, null, Ct);
				}
				{// 有變動之諸詞 處理 ChangedPart
					await UpdProps.AddRange(dtoSync.NeoOrChangedProps.ChangedPart, null, Ct);
					await UpdLearns.AddRange(dtoSync.NeoOrChangedLearns.ChangedPart, null, Ct);
					if(dtoSync.ChangedPoWord is not null){
						await UpdPoWords.Add(dtoSync.ChangedPoWord, Ct);
					}
				}
				if(Cfg.AutoUpdBizUpdatedAt && UpdatedWord.WordInDb is not null){
					await UpdUpd(UpdatedWord.WordInDb.Id_(), Ct);
				}
			}//~foreach(var UpdatedWord in DtoAddWords.UpdatedWords)
			sw.Stop();
			Logger.LogInformation($"SyncFromDto {sw.ElapsedMilliseconds}ms");
			return NIL;
		};
	}



	public async Task<Func<
		IUserCtx
		,DtoAddWordsOld
		,CT
		,Task<nil>
	>> FnAddOrUpdWordsFromTxtByDto(
		IDbFnCtx Ctx
		,CT Ct
	){
		var MergeWordsByDto = await FnMergeDtoAddWordsInToDbOld(Ctx, Ct);
		var InsertPoLearns = await DaoWord.FnInsertPoLearns(Ctx, Ct);
		var UpdUpd = await DaoWord.FnTriggerOnRootAfterUpd(Ctx, Ct);
		var R = async(
			IUserCtx UserCtx
			,DtoAddWordsOld DtoAddWords
			,CT Ct
		)=>{
			await using var NeoLearns = new BatchCollector<PoWordLearn, nil>(async (learns, Ct)=>{
				return await InsertPoLearns(null, learns, Ct);
			});

			//按新ʹProps 決 添加記錄
			foreach(var OneNonExisting in DtoAddWords.NeoWords){
				var NeoPoLearns = MkPoLearnList(OneNonExisting.Props, OneNonExisting.Id_());
				await NeoLearns.AddRange(NeoPoLearns, null, Ct);
			}
			foreach(var UpdatedWord in DtoAddWords.UpdatedWords){
				if(UpdatedWord.DiffedWord == null){
					continue;
				}
				var NeoPoLearns = MkPoLearnList(UpdatedWord.DiffedWord.Props, UpdatedWord.WordInDb.Id_());
				if(NeoPoLearns.Count > 0){
					await NeoLearns.AddRange(NeoPoLearns, null, Ct);
					await UpdUpd(UpdatedWord.WordInDb.Id_(), Ct);
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
		,Task<DtoAddWordsOld>
	>> FnAddOrUpdWordsFromTxt(
		IDbFnCtx Ctx
		,CT Ct
	){
		var ClassifyWordsToAdd = await FnClassifyWordsToAddOld(Ctx, Ct);
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
		,IEnumerable<IJnWord>
		,CT
		,Task<DtoAddWordsOld>
	>> FnAddEtMergeWordsOld(
		IDbFnCtx Ctx
		,CT Ct
	){
		var ClassifyWordsToAdd = await FnClassifyWordsToAddOld(Ctx, Ct);
		var AddOrUpdateWordsByDto = await FnMergeDtoAddWordsInToDbOld(Ctx,Ct);
		return async(UserCtx,JnWords,Ct)=>{
			JnWords = JnWords.Select(x=>{x.Word.Owner = UserCtx.UserId; return x;});
			var DtoAddWords = await ClassifyWordsToAdd(UserCtx, JnWords, Ct);
			await AddOrUpdateWordsByDto(UserCtx, DtoAddWords, Ct);
			return DtoAddWords;
		};
	}


	public async Task<Func<
		IUserCtx
		,IEnumerable<IJnWord>
		,CT
		,Task<DtoSyncWords>
	>> FnSyncWords(
		IDbFnCtx Ctx, CfgMerge Cfg
		,CT Ct
	){
		var ClassifyWordsToSync = await FnClassifyWordsToSync(Ctx, Ct);
		var AddOrUpdateWordsByDto = await FnSyncFromDto(Ctx, Cfg, Ct);
		return async(UserCtx,JnWords,Ct)=>{
			JnWords = JnWords.Select(x=>{x.Word.Owner = UserCtx.UserId; return x;});
			var DtoAddWords = await ClassifyWordsToSync(UserCtx, JnWords, Ct);
			await AddOrUpdateWordsByDto(UserCtx, DtoAddWords, Ct);
			return DtoAddWords;
		};
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
		var DelPoKvByWordIds = await RepoProp.FnSoftDelManyByKeys<IdWord>(Ctx, nameof(PoWordProp.WordId), 1000, Ct);
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
		var FnAddWords = await FnAddEtMergeWordsOld(Ctx, Ct);
		var R = async (
			IUserCtx User
			,IAsyncEnumerable<str> JsonLineIter
			,CT Ct
		)=>{
			await using var Bl = new BatchCollector<JnWord, nil>(async (words, Ct)=>{
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
	/// 以 IdWord 潙基準 改(詞頭, 語言)
	/// 返(詞頭,語言)對應之id
	/// 庫中無新改ʹ(詞頭,語言)則返源ʹ詞ʹid
	/// 若有新改ʹ(詞頭,語言) 即把源詞合併入目標詞後 返舊詞ʹid
	/// 傳入ʹIdWordˋ在庫中尋不見旹返null
	/// </summary>
	/// <exception cref="FatalLogicErr"></exception>
	[Obsolete("會直ᵈ改 標識、多端同步旹恐致謬")]
	public async Task<Func<
		IUserCtx
		,IdWord
		,str//head
		,str//lang
		,CT
		,Task<IdWord?> //詞頭對應之id
	>> FnUpdWordHeadLangOld(IDbFnCtx Ctx, CT Ct){
		var GetJnWordByIdEtCheckOwner = await FnGetJnWordByIdEtCheckOwner(Ctx, Ct);
		var UpdWordHeadLang = await DaoWord.FnUpdPoWordHeadLang(Ctx, Ct);
		var SlctIdByOwnerHeadLang = await DaoWord.FnSlctIdByOwnerHeadLangWithDel(Ctx, Ct);
		var MergeWordsIntoDb = await FnAddEtMergeWordsOld(Ctx, Ct);
		var SoftDelJnWordById = await FnSoftDelJnWordsByIds(Ctx,Ct);
		var UpdPropForeignWordIdById = await RepoProp.FnUpdOneColById(Ctx, nameof(PoWordProp.WordId), Ct);
		var UpdLearnForeignWordIdById = await RepoLearn.FnUpdOneColById(Ctx, nameof(PoWordLearn.WordId), Ct);
		//var UpdUpd = await DaoWord.FnTriggerOnRootAfterUpd(Ctx,Ct);

		return async (User, WordId, Head, Lang, Ct)=>{
			var WordOfId = await GetJnWordByIdEtCheckOwner(User, WordId, Ct);
			if(WordOfId is null){
				return null;
			}
			//var Lang = SrcWord.Lang;
			var IdOfWordWithHeadLang = await SlctIdByOwnerHeadLang(User, Head, Lang, Ct);
			if(IdOfWordWithHeadLang is null){
				await UpdWordHeadLang(User, WordId, Head, Lang, Ct);
				//await UpdUpd(IdWord, Ct);
				return WordId;
			}
			var WordOfHeadLang = await GetJnWordByIdEtCheckOwner(User, IdOfWordWithHeadLang.Value, Ct)
			?? throw new FatalLogicErr("Existing is null");
			var RawWordId = DaoWord.IdUpperToRaw<PoWord>(IdOfWordWithHeadLang);
			//await UpdUpd(TargetId.Value, Ct);
			foreach(var prop in WordOfId.Props){
				await UpdPropForeignWordIdById(prop.Id, RawWordId, Ct);
			}
			foreach(var learn in WordOfId.Learns){
				await UpdLearnForeignWordIdById(learn.Id, RawWordId , Ct);
			}
			await SoftDelJnWordById(User, [WordId], Ct);
			return IdOfWordWithHeadLang;
		};
	}


	/// <summary>
	/// 以 IdWord 潙基準 改(詞頭, 語言)
	/// 返ʹid 對應 新(詞頭,語言)
	/// 軟刪舊詞 建新詞、保證 原有ʹ Id->(Head, Lang)猶對應
	/// 傳入ʹIdWordˋ在庫中尋不見旹返null
	/// </summary>
	/// <exception cref="FatalLogicErr"></exception>
	public async Task<Func<
		IUserCtx
		,IdWord
		,str//head
		,str//lang
		,CT
		,Task<IdWord?> //新(詞頭,語言)對應之id
	>> FnUpdWordHeadLang(IDbFnCtx Ctx, CT Ct){
		var GetJnWordByIdEtCheckOwner = await FnGetJnWordByIdEtCheckOwner(Ctx, Ct);
		var SlctIdByOwnerHeadLang = await DaoWord.FnSlctIdByOwnerHeadLangWithDel(Ctx, Ct);
		var MergeWordsIntoDb = await FnAddEtMergeWordsOld(Ctx, Ct);
		var SoftDelJnWordById = await FnSoftDelJnWordsByIds(Ctx,Ct);
		var UpdUpd = await DaoWord.FnTriggerOnRootAfterUpd(Ctx,Ct);

		return async (User, WordId, Head, Lang, Ct)=>{
			var WordOfId = await GetJnWordByIdEtCheckOwner(User, WordId, Ct);
			if(WordOfId is null){
				return null;
			}
			var clonedWordOfId = WordOfId.DeepCloneEtResetIds();
			var IdOfWordWithHeadLang = await SlctIdByOwnerHeadLang(User, Head, Lang, Ct);
			IdWord IdToRtn = default;
			if(IdOfWordWithHeadLang is null){
				IdToRtn = clonedWordOfId.Id_();
			}else{
				var WordOfHeadLang = await GetJnWordByIdEtCheckOwner(User, IdOfWordWithHeadLang.Value, Ct)
				?? throw new FatalLogicErr("Existing is null");
				clonedWordOfId.SetIdEtEnsureFKey(WordOfHeadLang.Id_());
				IdToRtn = IdOfWordWithHeadLang.Value;
			}
			clonedWordOfId.Word.Head = Head;
			clonedWordOfId.Word.Lang = Lang;
			await MergeWordsIntoDb(User, [clonedWordOfId] , Ct);
			await UpdUpd(clonedWordOfId.Id_(), Ct);
			await SoftDelJnWordById(User, [WordId], Ct);
			return IdToRtn;
		};
	}



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
	/// //TODO Id或時間爲空時先自動填充
	/// TODO 返回實ʹ操作 如 無變, 有增 等
	public async Task<Func<
		IUserCtx
		,IJnWord
		,CT
		,Task<nil>
	>> FnUpdJnWord(IDbFnCtx Ctx, CT Ct){
		var CfgMerge = new CfgMerge{AutoUpdBizUpdatedAt=true};
		var GetJnWordByIdEtCheckOwner = await FnGetJnWordByIdEtCheckOwner(Ctx, Ct);
		var UpdWordHeadLang = await FnUpdWordHeadLang(Ctx, Ct);
		var SyncFromDto = await FnSyncFromDto(Ctx, CfgMerge, Ct);
		var MergeWordsIntoDb = await FnSyncWords(Ctx, CfgMerge, Ct);

		var SofeDelPropsByIds = await RepoProp.FnSoftDelManyByKeys<IdWordProp>(
			Ctx, nameof(PoWordProp.Id), 1000, Ct
		);
		var SofeDelLearnByIds = await RepoLearn.FnSoftDelManyByKeys<IdWordLearn>(
			Ctx, nameof(PoWordLearn.Id), 1000, Ct
		);
		//var UpdUpd = await DaoWord.FnTriggerOnRootAfterUpd(Ctx, Ct);
		return async(User, SimpleJnWord, Ct)=>{
			var JnWord = SimpleJnWord.AsOrToJnWord();
			var OldWord = await GetJnWordByIdEtCheckOwner(User, JnWord.Id, Ct);
			if(OldWord is null){//JnWord潙新詞(其Id不存于數據庫)
				//清洗ID、不 用ᵣ用戶ʃ輸
				JnWord.SetIdEtEnsureFKey(new IdWord());
				await MergeWordsIntoDb(User, [JnWord], Ct);
				return NIL;
			}else{//JnWord非新詞(其Id己存于數據庫)
				if(JnWord.Owner != User.UserId){
					throw ItemsErr.Word.__And__IsNotSameUserWord.ToErr();
				}
				if(JnWord.BizUpdatedAt != OldWord.BizUpdatedAt){
					JnWord.BizUpdatedAt = OldWord.BizUpdatedAt;
				}
				//(詞頭, 語言)ˉ標識 有變
				if(JnWord.Head != OldWord.Head || JnWord.Lang != OldWord.Lang){
					var amended = await UpdWordHeadLang(
						User, JnWord.Id, JnWord.Head, JnWord.Lang, Ct
					)
					?? throw new FatalLogicErr("Existing is null");
					// if(amended != OldWord.Id){
					// 	throw new FatalLogicErr("amended != OldWord.Id");
					// }
					JnWord.SetIdEtEnsureFKey(amended);
					//return NIL;// 不允 同時改標識(即(詞頭,語言)) 與他ʹ訊
				}

				OldWord.Head = JnWord.Head;//先改老詞ʹ (詞頭,語言)、否則後ʹdiff旹報錯曰非同一詞
				OldWord.Lang = JnWord.Lang;

				foreach(var Prop in JnWord.Props){
					if(Prop.Id.IsNullOrDefault()){//新增條目 則設ID潙0
						Prop.Id = new IdWordProp();
						Prop.BizCreatedAt = new Tempus();
					}
				}
				foreach(var Learn in JnWord.Learns){
					if(Learn.Id.Value.IsNullOrDefault()){
						Learn.Id = new IdWordLearn();
						Learn.BizCreatedAt = new Tempus();
					}
				}
				JnWord.EnsureForeignId();

				IJnWord? NeoDiffOld_ChangedPart = new JnWord{Word = JnWord.Word,}; //JnWord與OldWord不同之內容
				IJnWord? NeoDiffOld_SurplusPart = new JnWord{Word = JnWord.Word,}; //JnWord比OldWord多出之內容
				var NeoDiffOld = OldWord.Sync(JnWord, ref NeoDiffOld_SurplusPart, ref NeoDiffOld_ChangedPart);

				IJnWord? OldDiffNeo_ChangedPart = new JnWord{Word = JnWord.Word,};
				IJnWord? OldDiffNeo_SurplusPart = new JnWord{Word = JnWord.Word,};
				var OldDiffNeo = JnWord.Sync(OldWord, ref OldDiffNeo_SurplusPart, ref OldDiffNeo_ChangedPart);

				await SyncFromDto(User, NeoDiffOld.ToDtoSyncWords(OldWord), Ct);

				{//刪ᵣ (OldWord比JnWord多出之內容)

					await using var DelProps = new BatchCollector<IdWordProp, nil>(async(ids, Ct)=>{
						await SofeDelPropsByIds(ids, Ct);
						return NIL;
					});
					await using var DelLearns = new BatchCollector<IdWordLearn, nil>(async(ids, Ct)=>{
						return await SofeDelLearnByIds(ids, Ct);
					});
					foreach(var Prop in OldDiffNeo.NeoOrChangedProps.NeoPart??[]){
						await DelProps.Add(Prop.Id, Ct);
					}
					foreach(var Learn in OldDiffNeo.NeoOrChangedLearns.NeoPart??[]){
						await DelLearns.Add(Learn.Id, Ct);
					}
				}
			}
			return NIL;
		};
	}

	public async Task<Func<
		IUserCtx, DtoCompressedWords, CT, Task<nil>
	>> FnSyncFromCompressedWord(IDbFnCtx Ctx, CT Ct){
		var AddEtMergeWords = await FnSyncWords(Ctx, new CfgMerge{AutoUpdBizUpdatedAt=false}, Ct);
		return async(User, Dto, Ct)=>{
			var Words = await DecompressFromWordsJson(Dto, Ct);
			await AddEtMergeWords(User, Words, Ct);
			return NIL;
		};
	}

	public async Task<Func<
		IUserCtx, TextWithBlob, CT, Task<nil>
	>> FnSyncFromTextWithBlob(IDbFnCtx Ctx, CT Ct){
		var AddCompressedWord = await FnSyncFromCompressedWord(Ctx, Ct);
		return async(User, TextWithBlob, Ct)=>{
			var info = JSON.parse<WordsPackInfo>(TextWithBlob.Text);
			if(info is null){
				throw ItemsErr.Common.ArgErr.ToErr();
			}
			var Req = info.ToDtoCompressedWords(TextWithBlob.Blob.ToArray());
			await AddCompressedWord(User, Req, Ct);
			return NIL;
		};
	}
}
