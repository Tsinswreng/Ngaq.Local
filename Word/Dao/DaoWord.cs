using System.Collections;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Ngaq.Core.Model.Bo;
using Ngaq.Core.Model.Po;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Model.UserCtx;
using Ngaq.Core.Tools;
using Ngaq.Local.Db;

namespace Ngaq.Local.Dao;

[Obsolete]
public class DaoWord(
	LocalDbCtx DbCtx
){


	public async Task<Func<
		IUserCtx
		,str
		,str
		,CancellationToken
		,Task<IdWord?>
	>>
	FnSelectIdByFormIdEtLang(
		CancellationToken ct
	){
		return async (
			IUserCtx OperatorCtx
			,str FormId
			,str Lang
			,CancellationToken ct
		)=>{
			var UserId = OperatorCtx.UserId;
//Query wasn't precompiled and dynamic code isn't supported (NativeAOT))
			var ans = await DbCtx.Po_Word.AsNoTracking()
				.Where(w =>
					w.Head == FormId
					&& w.Lang == Lang
					&& w.CreatedBy == UserId
				)
				.Select(w =>(IdWord?)w.Id)
				//.DefaultIfEmpty(null)
				.FirstOrDefaultAsync(ct) //結構體之default非null
			;
			return ans;

		};
	}


	public async Task<Func<
		IdWord
		,CancellationToken
		,Task<JnWord?>
	>> FnSelectBoWordById(
		CancellationToken ct
	){
		var Fn = async(
			IdWord Id
			,CancellationToken ct
		)=>{
			var Po_Word = await DbCtx.Po_Word.AsNoTracking().Where(w => w.Id == Id).FirstOrDefaultAsync(ct);
			if(Po_Word == null){
				return null;
			}
			var Props = await DbCtx.Po_Kv.Where(
				w=>Id.Equals(w.WordId)
			).ToListAsync(ct);
			var Learns = await DbCtx.Po_Learn.Where(
				w=>Id.Equals(w.WordId)
			).ToListAsync(ct);
			var ans = new JnWord{
				PoWord = Po_Word
				,Props = Props
				,Learns = Learns
			};
			return ans;
		};
		return Fn;
	}


/// <summary>
/// //TODO 優化批量操作
/// </summary>
/// <returns></returns>
	public async Task<Func<
		IEnumerable<JnWord>
		,CancellationToken
		,Task<nil>
	>> FnInsertBoWords(
		CancellationToken ct
	) {
		var Fn = async(
			IEnumerable<JnWord> Bo_Words
			,CancellationToken ct
		)=>{
			u64 BatchSize = 0xfff;
			// List<Po_Word> Po_Words = [];
			// List<Po_Kv> Po_Kvs = [];
			// List<Po_Learn> Po_Learns = [];
			using var Po_Words = new BatchListAsy<PoWord, nil>(async(list, ct)=>{
				await DbCtx.Po_Word.AddRangeAsync(list, ct);
				return Nil;
			}, BatchSize);


			using var Po_Kvs = new BatchListAsy<PoKv, nil>(async(e, ct)=>{
				await DbCtx.Po_Kv.AddRangeAsync(e, ct);
				return Nil;
			}, BatchSize);


			using var Po_Learns = new BatchListAsy<PoLearn, nil>(async(e, ct)=>{
				await DbCtx.Po_Learn.AddRangeAsync(e, ct);
				return Nil;
			}, BatchSize);

			u64 i = 0;
			// var FnAddAsy = async()=>{
			// 	await DbCtx.Po_Word.AddRangeAsync(Po_Words, ct);
			// 	await DbCtx.Po_Kv.AddRangeAsync(Po_Kvs, ct);
			// 	await DbCtx.Po_Learn.AddRangeAsync(Po_Learns, ct);
			// 	//await DbCtx.SaveChangesAsync(ct);?
			// 	Po_Words.Clear();
			// 	Po_Kvs.Clear();
			// 	Po_Learns.Clear();
			// };

			foreach (var Bo_Word in Bo_Words) {
				await Po_Words.Add(Bo_Word.PoWord, ct);
				foreach (var Prop in Bo_Word.Props) {
					await Po_Kvs.Add(Prop, ct);
				}
				foreach (var Learn in Bo_Word.Learns) {
					await Po_Learns.Add(Learn, ct);
				}
				i++;
			}
			await Po_Words.End(ct);
			await Po_Kvs.End(ct);
			await Po_Learns.End(ct);
			return Nil;
		};
		return Fn;
	}

	public async Task<Func<
		IEnumerable<PoKv>
		,CancellationToken
		,Task<nil>
	>> FnInsertPoKvs(
		CancellationToken ct
	){
		var Fn = async(
			IEnumerable<PoKv> Po_Kvs
			,CancellationToken ct
		)=>{
			await DbCtx.Po_Kv.AddRangeAsync(Po_Kvs, ct);
			return Nil;
		};
		return Fn;
	}

	public async Task<Func<
		IEnumerable<T_Entity>
		,i64
		,CancellationToken
		,Task<nil>
	>> FnBatchSetUpdateAt<
		T_Entity
		,T_Id
	>(
		CancellationToken ct
	)where T_Entity : class, IPoBase, I_Id<T_Id>
	{
		var Fn = async(
			IEnumerable<T_Entity> Pos
			,i64 Time
			,CancellationToken ct
		)=>{
			foreach(var po in Pos){
				if(po is not I_Id<T_Id> IdPo){
					continue;
				}
				await DbCtx.Set<T_Entity>().Where(
					w=>((I_Id<T_Id>)w).Id!.Equals(IdPo.Id)
				).ExecuteUpdateAsync(s=>
					s.SetProperty(e=>e.UpdatedAt, Time), ct
				);
			}
			return Nil;
		};
		return Fn;
	}
}



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


	// public class SelectBoWordByIdAsy(Z z){
	// 	public async Task<SelectBoWordByIdAsy> InitAsy(){return this;}
	// 	public async Task<Bo_Word> RunAsy(
	// 		Id_Word Id
	// 		,CancellationToken ct
	// 	){
	// 		var DbCtx = z.DbCtx;
	// 		var Po_Word = await DbCtx.Po_Word.Where(w => w.Id == Id).FirstOrDefaultAsync(ct);
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
