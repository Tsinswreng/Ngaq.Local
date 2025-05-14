using System.Text;
using Microsoft.EntityFrameworkCore;
using Ngaq.Core.Infra;
using Ngaq.Core.Model.Bo;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Service.Parser;
using Ngaq.Core.Service.Word;
using Ngaq.Core.Stream;
using Ngaq.Core.Tools.Io;
using Ngaq.Local.Db;

namespace Ngaq.Local.Service.Word;

public class Svc_Word(
	// RepoFsql<Po_Word, Id_Word> Repo_Word
	// ,RepoFsql<Po_Kv, Id_Kv> Repo_Kv
	I_Svc_ParseWordList Svc_ParseWordList
	,DbCtx DbCtx
)
	//: I_Svc_AddWord
{
	public DbCtx DbCtx { get; set; } = DbCtx;

	//不能在接口中對內部類做約束
	// public class SelectIdByFormIdEtLangAsy(Svc_Word z){
	// 	public async Task<SelectIdByFormIdEtLangAsy> InitAsy(){return this;}
	// 	public async Task<Id_Word?> RunAsy(
	// 		str FormId
	// 		,str Lang
	// 		,CancellationToken ct
	// 	){
	// 		return await z.DbCtx.Po_Word.Where(w => w.WordFormId == FormId && w.Lang == Lang)
	// 			.Select(w => w.Id)
	// 			.FirstAsync(ct)
	// 		;
	// 	}
	// }

	public async Task<Func<
		str
		,str
		,CancellationToken
		,Task<Id_Word?>
	>>
	Fn_SelectIdByFormIdEtLangAsy(){
		return async (
			str FormId
			,str Lang
			,CancellationToken ct
		)=>{
			return await DbCtx.Po_Word.Where(w => w.WordFormId == FormId && w.Lang == Lang)
				.Select(w => w.Id)
				.FirstAsync(ct)
			;
		};
	}

	// public class SelectBoWordByIdAsy(Z z){
	// 	public async Task<SelectBoWordByIdAsy> InitAsy(){return this;}
	// 	public async Task<Bo_Word> RunAsy(
	// 		Id_Word Id
	// 		,CancellationToken ct
	// 	){
	// 		var DbCtx = z.DbCtx;
	// 		var Po_Word = await DbCtx.Po_Word.Where(w => w.Id == Id).FirstAsync(ct);
	// 		var Props = await DbCtx.Po_Kv.Where(
	// 			w=>Id.Equals(w.FKey_UInt128)
	// 		).ToListAsync(ct);
	// 		var Learns = await DbCtx.Po_Learn.Where(
	// 			w=>Id.Equals(w.FKey_UInt128)
	// 		).ToListAsync(ct);
	// 		var ans = new Bo_Word{
	// 			Po_Word = Po_Word
	// 			,Props = Props
	// 			,Learns = Learns
	// 		};
	// 		return ans;
	// 	}
	// }

	public async Task<Func<
		Id_Word
		,CancellationToken
		,Task<Bo_Word>
	>> Fn_SelectBoWordByIdAsy(){
		var Fn = async(
			Id_Word Id
			,CancellationToken ct
		)
		=>{
			var Po_Word = await DbCtx.Po_Word.Where(w => w.Id == Id).FirstAsync(ct);
			var Props = await DbCtx.Po_Kv.Where(
				w=>Id.Equals(w.FKey_UInt128)
			).ToListAsync(ct);
			var Learns = await DbCtx.Po_Learn.Where(
				w=>Id.Equals(w.FKey_UInt128)
			).ToListAsync(ct);
			var ans = new Bo_Word{
				Po_Word = Po_Word
				,Props = Props
				,Learns = Learns
			};
			return ans;
		};
		return Fn;
	}

	//TODO 返 初添ʹ詞ʹ 及 更新ʹ詞ʹID
	public async Task<Func<
		IEnumerable<Bo_Word>
		,CancellationToken
		,Task<nil>
	>> Fn_AddOrUpdateWordsAsy(
	){
		var SeekIdByFormEtLang = await Fn_SelectIdByFormIdEtLangAsy();
		//var SeekIdByFormEtLang = await new SelectIdByFormIdEtLangAsy(this).InitAsy();
		//var InsertBoWords = await new InsertBoWordsAsy(this).InitAsy();

		var Fn = async(
			IEnumerable<Bo_Word> Bo_Words
			,CancellationToken ct
		)=>{

			foreach (var Bo_Word in Bo_Words) {
				var ExistingId = await SeekIdByFormEtLang(
					Bo_Word.Po_Word.WordFormId
					,Bo_Word.Po_Word.Lang
					,ct
				);
				if(ExistingId == null){

				}
			}

			return Nil;

		};
		return Fn;
	}

	// public class InsertBoWordsAsy(Z z){
	// 	public async Task<InsertBoWordsAsy> InitAsy(){return this;}
	// 	public async Task<nil> RunAsy(
	// 		IEnumerable<Bo_Word> Bo_Words
	// 		,CancellationToken ct
	// 	){
	// 		var DbCtx = z.DbCtx;
	// 		u64 BatchSize = 0xfff;
	// 		List<Po_Word> Po_Words = [];
	// 		List<Po_Kv> Po_Kvs = [];
	// 		List<Po_Learn> Po_Learns = [];
	// 		u64 i = 0;
	// 		var FnAddAsy = async()=>{
	// 			await DbCtx.Po_Word.AddRangeAsync(Po_Words, ct);
	// 			await DbCtx.Po_Kv.AddRangeAsync(Po_Kvs, ct);
	// 			await DbCtx.Po_Learn.AddRangeAsync(Po_Learns, ct);
	// 			//await DbCtx.SaveChangesAsync(ct);?
	// 			Po_Words.Clear();
	// 			Po_Kvs.Clear();
	// 			Po_Learns.Clear();
	// 		};
	// 		foreach (var Bo_Word in Bo_Words) {
	// 			Po_Words.Add(Bo_Word.Po_Word);
	// 			foreach (var Prop in Bo_Word.Props) {
	// 				Po_Kvs.Add(Prop);
	// 			}
	// 			foreach (var Learn in Bo_Word.Learns) {
	// 				Po_Learns.Add(Learn);
	// 			}
	// 			if(i >= BatchSize){
	// 				await FnAddAsy();
	// 			}
	// 			i++;
	// 		}
	// 		if(Po_Words.Count > 0){//最後一批
	// 			await FnAddAsy();
	// 		}
	// 		return Nil;
	// 	}
	// }

	protected async Task<Func<
		IEnumerable<Bo_Word>
		,CancellationToken
		,Task<nil>
	>> Fn_InsertBoWordsAsy() {
		var Fn = async(
			IEnumerable<Bo_Word> Bo_Words
			,CancellationToken ct
		)=>{
			u64 BatchSize = 0xfff;
			List<Po_Word> Po_Words = [];
			List<Po_Kv> Po_Kvs = [];
			List<Po_Learn> Po_Learns = [];
			u64 i = 0;
			var FnAddAsy = async()=>{
				await DbCtx.Po_Word.AddRangeAsync(Po_Words, ct);
				await DbCtx.Po_Kv.AddRangeAsync(Po_Kvs, ct);
				await DbCtx.Po_Learn.AddRangeAsync(Po_Learns, ct);
				//await DbCtx.SaveChangesAsync(ct);?
				Po_Words.Clear();
				Po_Kvs.Clear();
				Po_Learns.Clear();
			};
			foreach (var Bo_Word in Bo_Words) {
				Po_Words.Add(Bo_Word.Po_Word);
				foreach (var Prop in Bo_Word.Props) {
					Po_Kvs.Add(Prop);
				}
				foreach (var Learn in Bo_Word.Learns) {
					Po_Learns.Add(Learn);
				}
				if(i >= BatchSize){
					await FnAddAsy();
				}
				i++;
			}
			if(Po_Words.Count > 0){//最後一批
				await FnAddAsy();
			}
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
