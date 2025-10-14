namespace Ngaq.Local.Word.Svc;
using Ngaq.Core.Infra.Core;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Model.Word.Dto;
using Ngaq.Core.Infra.Errors;
using Tsinswreng.CsPage;
using Ngaq.Core.Word.Models;
using Ngaq.Core.Infra;
using Ngaq.Core.Word.Models.Dto;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Word.Models.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Word.Models.Po.Learn;
using Tsinswreng.CsTools;
using Ngaq.Local.Db.TswG;
using Ngaq.Core.Domains.Base.Models;
using Ngaq.Core.Domains.User.UserCtx;

public partial class SvcWord{
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
				var Diffed = NewWord.DiffByTime(OldWord);
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

	public async Task<Func<
		IUserCtx
		,IPageQry
		,ReqSearchWord
		,CT
		,Task<IPage<ITypedObj>> //其Type可能潙 JnWord, DtoJnWordEtAsset
	>> FnPageSearch(IDbFnCtx Ctx, CT Ct){
		var PageSearchIdsByPrefix = await DaoWord.FnPageSearchWordIdsByHeadPrefix(Ctx, Ct);
		var CheckWordOwnerOrThrow = await FnGetJnWordByIdEtCheckOwner(Ctx, Ct);
		var SeekProp = await FnSlctPropEtJnWordByPropId(Ctx, Ct);
		var SeekLearn = await FnSlctLearnEtJnWordByLearnId(Ctx, Ct);
		return async (User, PageQry, Req, Ct)=>{
			var IdPage = await PageSearchIdsByPrefix(User, PageQry, Req, Ct);
			List<ITypedObj> Objs = [];
			try{
				var WordId = IdWord.FromLow64Base(Req.RawStr);
				var Word = await CheckWordOwnerOrThrow(User, WordId, Ct);
				if(Word is not null){
					Objs.Add(new TypedObj{
						Data = Word
						,Type = typeof(JnWord)
					});
				}
				var Prop_JnWord = await SeekProp(User, IdWordProp.FromLow64Base(Req.RawStr), Ct);
				if(Prop_JnWord is not null){
					Objs.Add(new TypedObj{
						Data = new DtoJnWordEtAsset{
							JnWord = Prop_JnWord.Value.Item2
							,Asset = Prop_JnWord.Value.Item1
						}
						,Type = typeof(PoWordProp)
					});
				}
				var Learn_JnWord = await SeekLearn(User, IdWordLearn.FromLow64Base(Req.RawStr), Ct);
				if(Learn_JnWord is not null){
					Objs.Add(new TypedObj{
						Data = new DtoJnWordEtAsset{
							JnWord = Learn_JnWord.Value.Item2
							,Asset = Learn_JnWord.Value.Item1
						}
						,Type = typeof(PoWordLearn)
					});
				}
			}catch (System.Exception){
				throw;
			}
			if(IdPage.DataAsyE is not null){
				await foreach(var IdWord in IdPage.DataAsyE){
					var Word = await CheckWordOwnerOrThrow(User, IdWord, Ct);//不應再拋異常
					if(Word is not null){
						Objs.Add(new TypedObj{
							Data = Word
							,Type = typeof(JnWord)
						});
					}
				}
			}
			var R = Page.Mk(PageQry, Objs);
			return R;
		};
	}

	public async Task<Func<
		IUserCtx
		,IPageQry
		,Tempus
		,CT
		,Task<IPage<JnWord>>
	>> FnPageChangedWordsWithDelWordsAfterTime(IDbFnCtx Ctx, CT Ct){
		var PageChangedIds = await DaoWord.FnPageChangedWordIdsWithDelWordsAfterTime(Ctx, Ct);
		var GetJnWordByIdEtCheckOwner = await FnGetJnWordByIdEtCheckOwner(Ctx, Ct);
		return async(User, PageQry, Tempus, Ct)=>{
			var IdPage = await PageChangedIds(User, PageQry, Tempus, Ct);
			var RList = new List<JnWord>();
			foreach(var id in IdPage.Data??[]){
				var U = await GetJnWordByIdEtCheckOwner(User, id, Ct);
				if(U is not null){
					RList.Add(U);
				}
			}
			var R = Page.Mk(PageQry, RList);
			return R;
		};
	}

	public async Task<Func<
		IUserCtx
		,IdWordProp
		,CT
		,Task<(PoWordProp,JnWord)?>
	>> FnSlctPropEtJnWordByPropId(IDbFnCtx Ctx, CT Ct){
		var SeekIdByPropId = await DaoWord.FnSlctRootIdByPropId(Ctx, Ct);
		var GetJnWordByIdEtCheckOwner = await FnGetJnWordByIdEtCheckOwner(Ctx, Ct);
		var SeekProp = await RepoKv.FnSlctById(Ctx, Ct);
		return async(User, PropId, Ct)=>{
			var WordId = await SeekIdByPropId(PropId, Ct);
			if(WordId is null){
				return null;
			}
			var JnWord = await GetJnWordByIdEtCheckOwner(User, WordId.Value, Ct);
			var Prop = await SeekProp(PropId, Ct);
			return (Prop!, JnWord!);
		};
	}

	public async Task<Func<
		IUserCtx
		,IdWordLearn
		,CT
		,Task<(PoWordLearn,JnWord)?>
	>> FnSlctLearnEtJnWordByLearnId(IDbFnCtx Ctx, CT Ct){
		var SeekIdByLearnId = await DaoWord.FnSlctRootIdByLearnId(Ctx, Ct);
		var GetJnWordByIdEtCheckOwner = await FnGetJnWordByIdEtCheckOwner(Ctx, Ct);
		var SeekLearn = await RepoLearn.FnSlctById(Ctx, Ct);
		return async(User, LearnId, Ct)=>{
			var WordId = await SeekIdByLearnId(LearnId, Ct);
			if(WordId is null){
				return null;
			}
			var JnWord = await GetJnWordByIdEtCheckOwner(User, WordId.Value, Ct);
			var Learn = await SeekLearn(LearnId, Ct);
			return (Learn!, JnWord!);
		};
	}

}
