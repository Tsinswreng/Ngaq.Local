namespace Ngaq.Local.Domains.Word.Svc;
using Ngaq.Core.Infra.Core;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Infra.Errors;
using Tsinswreng.CsPage;
using Ngaq.Core.Infra;
using Ngaq.Core.Word.Models.Dto;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Tsinswreng.CsTools;
using Ngaq.Local.Db.TswG;
using Ngaq.Core.Shared.Base.Models;
using Ngaq.Core.Shared.User.UserCtx;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Shared.Word.Models;
using Ngaq.Core.Shared.Word.Models.Po.Learn;
using Ngaq.Core.Shared.Word.Models.Dto;
using Ngaq.Core.Tools;
using System.IO.Compression;
using System.Text;
using System.Collections;
using Tsinswreng.CsSqlHelper;
using Ngaq.Local.Domains.Word.Dao;
using Ngaq.Core.Shared.Word;

public partial class SvcWord{
public async Task<Func<
		IUserCtx
		,IEnumerable<IJnWord>
		,CT
		,Task<DuplicationGroup<IJnWord>>
	>> FnGroupByExising(
		IDbFnCtx Ctx
		,CT Ct
	){
		var SeekIdByHeadEtLang = await DaoWord.FnSlctIdByOwnerHeadLang(Ctx, Ct);
		var SeekJnWordById = await DaoWord.FnSlctJnWordByIdWithDel(Ctx, Ct);
		return async(UserCtx, JnWords, Ct)=>{
			var NonExistingList = new List<IJnWord>();
			var ExiDupliPairs = new List<Existing_Duplication<IJnWord>>();
			foreach(var (i,JnWord) in JnWords.Index()){
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
					var ExiDupliPair = new Existing_Duplication<IJnWord>(
						Existing: JnWordInDb
						,Duplication: JnWord
					);
					ExiDupliPairs.Add(ExiDupliPair);
				}
			}
			var R = new DuplicationGroup<IJnWord>();
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
		,IEnumerable<IJnWord>
		,CT
		,Task<DtoAddWordsOld>
	>> FnClassifyWordsToAddOld(
		IDbFnCtx Ctx
		,CT Ct
	){
		var GroupByExisting = await FnGroupByExising(Ctx, Ct);

		return async(UserCtx,JnWords,Ct)=>{
			var R = new DtoAddWordsOld();

			//按語言與詞頭分類
			var HeadLang_Words = JnWords.GroupByLangHead();

			//合併後ʹ諸詞。斯列表中 同語言同詞頭之詞當只出現一次
			var Mergeds = new List<IJnWord>();
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
			var ChangedNewWords = new List<IJnWord>();
			foreach(var Exi_Dupli in ExistGroup.Existing_Duplications??[]){
				var OldWord = Exi_Dupli.Existing;//庫中已有ʹ舊詞
				var NewWord = Exi_Dupli.Duplication;//待加ʹ新詞

				var Diffed = NewWord.DiffByTime(OldWord);
				if(Diffed == null){
					continue;
				}
				var DtoUpdatedWord = new DtoUpdWordOld(
					WordInDb: OldWord
					,WordToAdd: NewWord
					,DiffedWord: Diffed
				);

				R.UpdatedWords.Add(DtoUpdatedWord);
			}
			return R;
		};
	}

	/// <summary>
	/// 按是否既存于庫中 蔿 待合入之諸詞 分類
	/// </summary>
	/// <returns></returns>
	public async Task<Func<
		IUserCtx,IEnumerable<IJnWord>,CT
		,Task<DtoSyncWords>
	>> FnClassifyWordsToSync(
		IDbFnCtx Ctx
		,CT Ct
	){
		var GroupByExisting = await FnGroupByExising(Ctx, Ct);

		return async(UserCtx,JnWords,Ct)=>{
			var R = new DtoSyncWords();

			//按語言與詞頭分類
			var HeadLang_Words = JnWords.GroupByLangHead();

			//合併後ʹ諸詞。斯列表中 同語言同詞頭之詞當只出現一次
			var Mergeds = new List<IJnWord>();
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
			var ChangedNewWords = new List<IJnWord>();
			foreach(var Exi_Dupli in ExistGroup.Existing_Duplications??[]){
				var OldWord = Exi_Dupli.Existing;//庫中已有ʹ舊詞
				var NewWord = Exi_Dupli.Duplication;//待加ʹ新詞

				IJnWord? NeoPart = new JnWord{Word = OldWord.Word};
				IJnWord? ChangedPart = new JnWord{Word = OldWord.Word};
				var SycnResult = OldWord.Sync(NewWord, ref NeoPart, ref ChangedPart);
				//var Diffed = NewWord.DiffByTime(OldWord);

				var UpdatedWord = SycnResult.ToDtoSyncWords(OldWord).UpdatedWords.FirstOrDefault();
				if(UpdatedWord is not null){
					R.UpdatedWords.Add(UpdatedWord);
				}
			}
			return R;
		};
	}



	public async Task<Func<
		IUserCtx
		,IPageQry
		,CT
		,Task<IPageAsyE<IJnWord>>
	>> FnPageWords(
		IDbFnCtx Ctx
		,CT Ct
	){
		return await DaoWord.FnPageWords(
			Ctx
			,new CfgQry{
				IncludeDeleted = false
			}
			,Ct
		);
	}

	public async Task<Func<
		IUserCtx
		,IPageQry
		,CT
		,Task<IPageAsyE<IJnWord>>
	>> FnPageWordsWithDel(
		IDbFnCtx Ctx
		,CT Ct
	){
		return await DaoWord.FnPageWords(
			Ctx
			,new CfgQry{
				IncludeDeleted = true
			}
			,Ct
		);
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
		var SelectJnWordById = await DaoWord.FnSlctJnWordByIdWithDel(Ctx, Ct);
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
				throw ItemsErr.Word.__And__IsNotSameUserWord.ToErr();
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
		,Task<IPage<IJnWord>>
	>> FnSearchWord(IDbFnCtx Ctx, CT Ct){
		var PageSearchIdsByPrefix = await DaoWord.FnPageSearchWordIdsByHeadPrefixWithDel(Ctx, Ct);
		var CheckWordOwnerOrThrow = await FnGetJnWordByIdEtCheckOwner(Ctx, Ct);
		return async (User, PageQry, Req, Ct)=>{
			var IdPage = await PageSearchIdsByPrefix(User, PageQry, Req, Ct);
			List<IJnWord> Words = [];
			try{
				var WordId = IdWord.FromLow64Base(Req.RawStr);
				var Word = await CheckWordOwnerOrThrow(User, WordId, Ct);
				if(Word is not null){
					Words.Add(Word);
				}
			}catch (Exception) {
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
		var PageSearchIdsByPrefix = await DaoWord.FnPageSearchWordIdsByHeadPrefixWithDel(Ctx, Ct);
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
			}catch (Exception) {
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
		,Task<IPage<IJnWord>>
	>> FnPageChangedWordsWithDelWordsAfterTime(IDbFnCtx Ctx, CT Ct){
		var PageChangedIds = await DaoWord.FnPageChangedWordIdsWithDelWordsAfterTime(Ctx, Ct);
		var GetJnWordByIdEtCheckOwner = await FnGetJnWordByIdEtCheckOwner(Ctx, Ct);
		return async(User, PageQry, Tempus, Ct)=>{
			var IdPage = await PageChangedIds(User, PageQry, Tempus, Ct);
			var RList = new List<IJnWord>();
			if(IdPage.DataAsyE is not null){
				await foreach(var id in IdPage.DataAsyE){
					var U = await GetJnWordByIdEtCheckOwner(User, id, Ct);
					if(U is not null){
						RList.Add(U);
					}
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
		var SeekProp = await RepoProp.FnSlctById(Ctx, Ct);
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

	/// <summary>
	/// 蔿臨時 速ᵈ上線
	/// </summary>
	/// <param name="Ctx"></param>
	/// <param name="Ct"></param>
	/// <returns></returns>
	public async Task<Func<
		IUserCtx, ReqPackWords, CT, Task<DtoCompressedWords>
	>> FnZipAllWordsJsonNoStream(IDbFnCtx Ctx, CT Ct){
		var FnPage = await FnPageWordsWithDel(Ctx, Ct);
		return async(User, Req, Ct)=>{
			if(Req.Type != EWordsPack.LineSepJnWordJsonGZip){
				throw new NotSupportedException();
			}
			var PageAll = await FnPage(User, PageQry.SlctAll(), Ct);
			var Jsons = new List<str>();
			if(PageAll.DataAsyE is not null){
				await foreach(var JnWord in PageAll.DataAsyE){
					Jsons.Add(JSON.stringify(JnWord));
				}
			}
			var Json = str.Join('\n', Jsons);
			var Compressed = CompressGZip(Encoding.UTF8.GetBytes(Json));
			return new DtoCompressedWords{
				Data = Compressed
				,Type = EWordsPack.LineSepJnWordJsonGZip
			};
		};
	}

	public async Task<Func<
		IUserCtx, ReqPackWords, CT, Task<TextWithBlob>
	>> FnPackAllWordsToTextWithBlobNoStream(IDbFnCtx Ctx, CT Ct){
		var ZipFn = await FnZipAllWordsJsonNoStream(Ctx, Ct);
		return async(User, Req, Ct)=>{
			var DtoCompressed = await ZipFn(User, Req, Ct);
			var packInfo = DtoCompressed.ToOrAssWordsPackInfo();
			var textWithBlob = ToolTextWithBlob.Pack(
				JSON.stringify(packInfo), DtoCompressed.Data
			);
			return textWithBlob;
		};
	}

	public async Task<IEnumerable<IJnWord>> DecompressFromWordsJson(
		DtoCompressedWords Compressed, CT Ct
	){
		try{
			if(Compressed.Type == EWordsPack.JnWordArrJsonGZip){
				var decompressedBytes = Decompress(Compressed.Data??[]);
				var Json = Encoding.UTF8.GetString(decompressedBytes);
				return JSON.parse<IList<JnWord>>(Json)!;
			}else if(Compressed.Type == EWordsPack.LineSepJnWordJsonGZip){
				var decompressedBytes = Decompress(Compressed.Data??[]);
				var Json = Encoding.UTF8.GetString(decompressedBytes);
				var Lines = Json.Split('\n');
				var JnWords = new List<IJnWord>();
				foreach(var line in Lines){
					if(str.IsNullOrWhiteSpace(line)){
						continue;
					}
					var JnWord = JSON.parse<JnWord>(line);
					if(JnWord is null){
						continue;
					}
					JnWords.Add(JnWord);
				}
				return JnWords;
			}else{
				throw new NotImplementedException();
			}
		}catch (System.Exception){

			throw;
		}
	}


	public static byte[] CompressGZip(byte[] data){
		using var output = new MemoryStream();
		using (var gzip = new GZipStream(output, CompressionMode.Compress)){
			gzip.Write(data, 0, data.Length);
		}
		return output.ToArray();
	}

	public static byte[] Decompress(byte[] compressedData){
		using var input = new MemoryStream(compressedData);
		using var gzip = new GZipStream(input, CompressionMode.Decompress);
		using var output = new MemoryStream();
		gzip.CopyTo(output);
		return output.ToArray();
	}

}
