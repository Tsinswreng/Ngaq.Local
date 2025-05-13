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
	public async Task<Bo_Word> SelectByIdAsy(
		Id_Word Id
		,CancellationToken ct = default
	){
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
	}

	public async Task<nil> AddBoWordsAsy(
		IEnumerable<Bo_Word> Bo_Words
		,CancellationToken ct = default
	) {
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
	}


	public async Task<nil> AddWordsFromFilePathAsy(Path_Encode Path_Encode) {
		I_Answer<nil> ans = new Answer<nil>();
		var WordsAns = await Svc_ParseWordList.ParseWordsFromFilePathAsy(Path_Encode);
		var Words = WordsAns.DataOrThrow();
		return Nil;
	}

	public async Task<nil> AddWordsFromTextAsy(string Text) {

		return Nil;
	}

	public async Task<nil> AddWordsFromUrlAsy(string Path) {

		return Nil;
	}
}
