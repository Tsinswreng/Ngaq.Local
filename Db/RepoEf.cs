using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Ngaq.Core.Infra;
using Ngaq.Core.Infra.Core;
using Ngaq.Core.Infra.Db;
using Ngaq.Core.Model;
using Ngaq.Core.Model.Bo;
using Ngaq.Core.Model.Po;
using Ngaq.Core.Model.Po.Kv;

namespace Ngaq.Local.Db;
//using T = Bo_Word;
public class RepoEf
<
[DynamicallyAccessedMembers(
	DynamicallyAccessedMemberTypes.PublicConstructors |
	DynamicallyAccessedMemberTypes.NonPublicConstructors |
	DynamicallyAccessedMemberTypes.PublicFields |
	DynamicallyAccessedMemberTypes.NonPublicFields |
	DynamicallyAccessedMemberTypes.PublicProperties |
	DynamicallyAccessedMemberTypes.NonPublicProperties |
	DynamicallyAccessedMemberTypes.Interfaces
)]
	T_Entity
	,T_Id
>
	:I_TxnAsyFnRunner
	where T_Entity: class, I_HasId<T_Id>
	where T_Id : IEquatable<T_Id>

{

	public RepoEf(DbCtx dbCtx){
		this.DbCtx = dbCtx;
	}

	public DbCtx DbCtx{get;set;}

	public async Task<Func<
		IEnumerable<T_Entity>
		,CancellationToken
		,Task<nil>
	>> Fn_InsertManyAsy(CancellationToken ct){
		var Fn = async(
			IEnumerable<T_Entity> Entitys
			,CancellationToken ct
		)=>{
			await DbCtx.Set<T_Entity>().AddRangeAsync(Entitys, ct);
			//TODO saveChange?
			return Nil;
		};
		return Fn;
	}

	public async Task<Func<
		T_Id2
		,CancellationToken
		,Task<T_Entity?>
	>> Fn_SeekByIdAsy<T_Id2>(CancellationToken ct){
		var Fn = async(
			T_Id2 Id
			,CancellationToken ct
		)=>{
			if(Id is not T_Id id){
				throw new FatalLogicErr("Id is not T_Id id");
			}
			var entity = await DbCtx.Set<T_Entity>().AsNoTracking().Select(x=>x)
				//.Where(x=>x.Id==id) // 运算符“==”无法应用于“T_Id”和“T_Id”类型的操作数CS0019
				.Where(x=>x.Id.Equals(id))
				.FirstOrDefaultAsync(ct)
			;
			return entity;
		};
		return Fn;
	}

	public async Task<Func<
		IEnumerable<Id_Dict<T_Id2>>
		,CancellationToken
		,Task<nil>
	>> Fn_UpdateManyAsy<T_Id2>(
		CancellationToken ct
	){
		var Fn = async(
			IEnumerable<Id_Dict<T_Id2>> Id_Dicts
			,CancellationToken ct
		)=>{
			var DbSet = DbCtx.Set<T_Entity>();
			foreach(var id_dict in Id_Dicts){
				var id = id_dict.Id;
				var dict = id_dict.Dict;
				var Old = await DbSet.Select(x=>x).Where(x=>x.Id.Equals(id)).FirstOrDefaultAsync(ct);
				if(Old == null){
					continue;
				}
				DictCtx.Assign(Old, dict);//源生成器實現、無反射
				if(Old is I_PoBase po){
					po.UpdatedAt = DateTimeOffset.Now.ToUnixTimeMilliseconds();
				}
			}//~for
			await DbCtx.SaveChangesAsync(); //TODO ?
			return Nil;
		};
		return Fn;
	}

	// public async Task<I_Answer<nil>> UpdateManyAsy(IEnumerable<T_Entity> EntityList){
	// 	I_Answer<nil> ans = new Answer<nil>();
	// 	IDbContextTransaction tx = null!;
	// 	//DbCtx.Entry
	// 	try{
	// 		tx = await DbCtx.Database.BeginTransactionAsync();

	// 		foreach (var newEntity in EntityList){
	// 			var existingEntity = DbCtx.Set<T_Entity>().Find(newEntity.Id);
	// 			if (existingEntity == null){
	// 				break;
	// 			}
	// 			foreach (var property in DbCtx.Entry(existingEntity).Properties){
	// 				var newValue = newEntity.GetType().GetProperty(property.Metadata.Name)?.GetValue(newEntity);
	// 				if (!Equals(property.CurrentValue, newValue)){
	// 					property.CurrentValue = newValue;
	// 					property.IsModified = true;
	// 				}
	// 			}
	// 		}

	// 		await DbCtx.SaveChangesAsync();
	// 		await tx.CommitAsync();
	// 		return ans.OkWith(Nil);
	// 	}
	// 	catch (Exception e){
	// 		ans.AddErrException(e);
	// 		await tx.RollbackAsync();
	// 	}
	// 	return ans;
	// }

	public async Task<Func<
		IEnumerable<object>
		,CancellationToken
		,Task<nil>
	>> Fn_DeleteManyByIdAsy(){
		var Fn = async(
			IEnumerable<object> IdList
			,CancellationToken ct
		)=>{
			await DbCtx.Set<T_Entity>()
				.Where(e=>IdList.Contains(e.Id))
				.ExecuteDeleteAsync(ct)
			;//仅生成一条 DELETE FROM ... WHERE Id IN (1, 2, 3) SQL 语句、Contains(e.Id)並非在內存中查找
			return Nil;
		};
		return Fn;
	}


	public async Task<T_Ret> TxnAsy<T_Ret>(
		Func<CancellationToken, Task<T_Ret>> FnAsy
		,CancellationToken ct
	){
		using var Tx = await DbCtx.Database.BeginTransactionAsync(ct);
		try{
			var ans = await FnAsy(ct);
			await Tx.CommitAsync(ct);
			return ans;
		}
		catch (System.Exception){
			await Tx.RollbackAsync(ct);
			throw;
		}
	}


}
