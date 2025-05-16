using System.Collections;
using Microsoft.EntityFrameworkCore;
using Ngaq.Core.Model.Bo;
using Ngaq.Core.Model.Po;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn;
using Ngaq.Core.Model.Po.User;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Model.UserCtx;
using Ngaq.Core.Stream;
using Ngaq.Local.Db;

namespace Ngaq.Local.Dao;

public class Dao_Word(
	DbCtx DbCtx
){


	public async Task<Func<
		I_UserCtx
		,str
		,str
		,CancellationToken
		,Task<Id_Word?>
	>>
	Fn_SelectIdByFormIdEtLangAsy(
		CancellationToken ct
	){
		return async (
			I_UserCtx OperatorCtx
			,str FormId
			,str Lang
			,CancellationToken ct
		)=>{
			var UserId = OperatorCtx.UserId;
			return await DbCtx.Po_Word.Where(w =>
				w.WordFormId == FormId
				&& w.Lang == Lang
				&& w.CreatedBy == UserId
			)
				.Select(w => w.Id)
				.FirstAsync(ct)
			;
		};
	}


	public async Task<Func<
		Id_Word
		,CancellationToken
		,Task<Bo_Word>
	>> Fn_SelectBoWordByIdAsy(
		CancellationToken ct
	){
		var Fn = async(
			Id_Word Id
			,CancellationToken ct
		)=>{
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


/// <summary>
/// //TODO 優化批量操作
/// </summary>
/// <returns></returns>
	public async Task<Func<
		IEnumerable<Bo_Word>
		,CancellationToken
		,Task<nil>
	>> Fn_InsertBoWordsAsy(
		CancellationToken ct
	) {
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

	public async Task<Func<
		IEnumerable<Po_Kv>
		,CancellationToken
		,Task<nil>
	>> Fn_InsertPoKvsAsy(
		CancellationToken ct
	){
		var Fn = async(
			IEnumerable<Po_Kv> Po_Kvs
			,CancellationToken ct
		)=>{
			await DbCtx.Po_Kv.AddRangeAsync(Po_Kvs, ct);
			return Nil;
		};
		return Fn;
	}

	public async Task<Func<
		IEnumerable<I_PoBase>
		,i64
		,CancellationToken
		,Task<nil>
	>> Fn_BatchSetUpdateAtAsy<T_Id>(
		CancellationToken ct
	){
		var Fn = async(
			IEnumerable<I_PoBase> Pos
			,i64 Time
			,CancellationToken ct
		)=>{
			foreach(var po in Pos){
				if(po is not I_Id<T_Id> IdPo){
					continue;
				}
				await DbCtx.Set<I_PoBase>().Where(
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
