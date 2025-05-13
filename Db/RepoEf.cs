using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Ngaq.Core.Infra;
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
	:I_Crud<T_Entity>
	where T_Entity: class, I_Id<T_Id>
	where T_Id : IEquatable<T_Id>
{



	public RepoEf(DbCtx dbCtx){
		this.DbCtx = dbCtx;
	}

	public DbCtx DbCtx{get;set;}

	public async Task<I_Answer<nil>> AddManyAsy(IEnumerable<T_Entity> Entitys){
		I_Answer<nil> ans = new Answer<nil>();
		IDbContextTransaction tx = null!;
		try{
			tx = await DbCtx.Database.BeginTransactionAsync();
			await DbCtx.Set<T_Entity>().AddRangeAsync(Entitys);
			await DbCtx.SaveChangesAsync();
			await tx.CommitAsync();
			return ans.OkWith(Nil);
		}
		catch (Exception e){
			ans.AddErrException(e);
			await tx.RollbackAsync();
		}
		return ans;
	}

	public async Task<I_Answer<T_Entity?>> SeekByIdAsy<T_Id2>(T_Id2 Id){
		I_Answer<T_Entity?> ans = new Answer<T_Entity?>();
		if(Id is not T_Id id){
			throw new FatalLogicErr("Id is not T_Id id");
			//return ans;
		}

		var entity = await DbCtx.Set<T_Entity>().Select(x=>x)
			//.Where(x=>x.Id==id) // 运算符“==”无法应用于“T_Id”和“T_Id”类型的操作数CS0019
			.Where(x=>x.Id.Equals(id))
			.FirstOrDefaultAsync()
		;
		if(entity == null){
			return ans.OkWith();
		}
		return ans.OkWith(entity);

	}

	public async Task<I_Answer<nil>> UpdateManyAsy<T_Id2>(
		IEnumerable<Id_Dict<T_Id2>> Id_Dicts
	){
		I_Answer<nil> ans = new Answer<nil>();
		IDbContextTransaction tx = null!;
		try{
			var DbSet = DbCtx.Set<T_Entity>();
			tx = await DbCtx.Database.BeginTransactionAsync();
			foreach(var id_dict in Id_Dicts){
				var id = id_dict.Id;
				var dict = id_dict.Dict;
				var Old = DbSet.Select(x=>x).Where(x=>x.Id.Equals(id)).FirstOrDefault();
				if(Old == null){
					continue;
				}
				DictCtx.Assign(Old, dict);//源生成器實現、無反射
				if(Old is I_PoBase po){
					po.UpdatedAt = DateTimeOffset.Now.ToUnixTimeMilliseconds();
				}
			}//~for
			await DbCtx.SaveChangesAsync();
			await tx.CommitAsync();
			ans.Ok = true;
		}
		catch (System.Exception e){
			await tx.RollbackAsync();
			ans.AddErrException(e);
		}
		return ans;
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

	public async Task<I_Answer<nil>> DeleteManyByIdAsy(IEnumerable<object> IdList){
		I_Answer<nil> ans = new Answer<nil>();
		IDbContextTransaction tx = null!;
		try{
			tx = await DbCtx.Database.BeginTransactionAsync();
			await DbCtx.Set<T_Entity>()
				.Where(e=>IdList.Contains(e.Id))
				.ExecuteDeleteAsync()
			;//仅生成一条 DELETE FROM ... WHERE Id IN (1, 2, 3) SQL 语句、Contains(e.Id)並非在內存中查找
			await tx.CommitAsync();
			return ans.OkWith(Nil);
		}
		catch (Exception e){
			ans.AddErrException(e);
			await tx.RollbackAsync();
		}
		return ans;
	}
}
