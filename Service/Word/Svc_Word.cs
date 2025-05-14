using System.Text;
using Microsoft.EntityFrameworkCore;
using Ngaq.Core.Infra;
using Ngaq.Core.Model;
using Ngaq.Core.Model.Bo;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Service.Parser;
using Ngaq.Core.Service.Word;
using Ngaq.Core.Stream;
using Ngaq.Core.Tools;
using Ngaq.Core.Tools.Io;
using Ngaq.Local.Dao;
using Ngaq.Local.Db;

namespace Ngaq.Local.Service.Word;

//不在Svc中依賴DbCtx
public class Svc_Word(
	I_Svc_ParseWordList Svc_ParseWordList
	,Dao_Word Dao_Word
)
	//: I_Svc_AddWord
{
	//public DbCtx DbCtx { get; set; } = DbCtx;

	public static Po_Word SetPoWordOwner(
		I_UserCtx OperatorCtx
		,Po_Word Po_Word
	){
		Po_Word.Owner = OperatorCtx.UserId;
		return Po_Word;
	}

	//TODO 返 初添ʹ詞ʹ 及 更新ʹ詞ʹID
	public async Task<Func<
		I_UserCtx
		,IEnumerable<Bo_Word>
		,CancellationToken
		,Task<nil>
	>> Fn_AddOrUpdateWordsAsy(
	){
		var SeekIdByFormEtLang = await Dao_Word.Fn_SelectIdByFormIdEtLangAsy();
		var InsertBoWords = await Dao_Word.Fn_InsertBoWordsAsy();
		var InsertPoKvs = await Dao_Word.Fn_InsertPoKvsAsy();
		var SelectBoWordById = await Dao_Word.Fn_SelectBoWordByIdAsy();
		var BatchSetUpdateAt = await Dao_Word.Fn_BatchSetUpdateAtAsy<Id_Word>();
		//var SeekIdByFormEtLang = await new SelectIdByFormIdEtLangAsy(this).InitAsy();
		//var InsertBoWords = await new InsertBoWordsAsy(this).InitAsy();
		var Fn = async(
			I_UserCtx OperatorCtx
			,IEnumerable<Bo_Word> Bo_Words
			,CancellationToken ct
		)=>{
			u64 BatchSize = 0xfff;

			//List<Bo_Word> NeoWords = [];
			//List<Po_Kv> NeoProps = [];
			//List<Po_Word> ChangedPoWords = [];
			using var NeoWords = new BatchListAsy<Bo_Word, nil>(InsertBoWords, BatchSize);
			using var NeoProps = new BatchListAsy<Po_Kv, nil>(InsertPoKvs, BatchSize);
			using var ChangedPoWords = new BatchListAsy<Po_Word, nil>(async (e,ct)=>{
				await BatchSetUpdateAt(e, DateTimeOffset.Now.ToUnixTimeMilliseconds() ,ct);return Nil;
			}, BatchSize);

			foreach (var Bo_Word in Bo_Words) {
				SetPoWordOwner(OperatorCtx, Bo_Word.Po_Word);
				var ExistingId = await SeekIdByFormEtLang(
					OperatorCtx
					,Bo_Word.Po_Word.WordFormId
					,Bo_Word.Po_Word.Lang
					,ct
				);
				var hasChanged = false;
				if(ExistingId == null){//新詞
					await NeoWords.AddAsy(Bo_Word,ct);
					hasChanged = true;
				}else{//老詞
					Bo_Word.Id = ExistingId.Value;
					var OldBo_Word = await SelectBoWordById(ExistingId.Value, ct);
					var NewProps = Bo_Word.DiffProps(Bo_Word.Props, OldBo_Word.Props);
					if(NewProps.Count == 0){
						hasChanged = false;
					}else{
						foreach(var Po_Kv in NewProps){
							Po_Kv.FKey_UInt128 = Bo_Word.Id.Value;
							await NeoProps.AddAsy(Po_Kv, ct);
						}
					}
				}
				if(hasChanged){
					var Po_Learn = new Po_Learn{
						FKey_UInt128 = Bo_Word.Id.Value
						//,KStr = Const_PropKey.
						,VDescr = Const_Learn.add
					};
					Bo_Word.Learns.Add(Po_Learn);
					Bo_Word.Po_Word.UpdatedAt = DateTimeOffset.Now.ToUnixTimeMilliseconds();
					await ChangedPoWords.AddAsy(Bo_Word.Po_Word, ct);
				}//~if(hasChanged)
			}//~for
			await NeoWords.EndAsy(ct);
			await NeoProps.EndAsy(ct);
			await ChangedPoWords.EndAsy(ct);
			return Nil;
		};
		return Fn;
	}

//TODO
	public async Task<nil> AddWordsFromFilePathAsy(Path_Encode Path_Encode) {
		I_Answer<nil> ans = new Answer<nil>();
		var Words = await Svc_ParseWordList.ParseWordsFromFilePathAsy(Path_Encode);

		foreach (var Bo_Word in Words) {
			var Po_Word = Bo_Word.Po_Word;
			//var Existing = await SelectByFormIdEtLangAsy(Po_Word.WordFormId, Po_Word.Lang);
		}
		return Nil;
	}

	public async Task<nil> AddWordsFromTextAsy(string Text) {

		return Nil;
	}

	public async Task<nil> AddWordsFromUrlAsy(string Path) {

		return Nil;
	}
}
