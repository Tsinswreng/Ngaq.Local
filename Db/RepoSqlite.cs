// namespace Ngaq.Local.Db;

// using System.Data;
// using System.Text.Json;
// using System.Text.Json.Serialization.Metadata;
// using Ngaq.Core.Infra.Core;
// using Ngaq.Core.Infra.Db;
// using Ngaq.Core.Model.Po;
// using Microsoft.Data.Sqlite;
// using Ngaq.Core.Infra;
// using Tsinswreng.SqlHelper;
// using Ngaq.Core.Model;
// using System.Collections;


// //using T = Bo_Word;
// public class RepoSqlite
// <
// 	T_Entity
// 	,T_Id
// >
// 	where T_Entity: class, I_Id<T_Id>
// 	where T_Id : IEquatable<T_Id>

// {

// 	public RepoSqlite(
// 		I_TableMgr TblMgr
// 	){
// 		this.TblMgr = TblMgr;
// 	}

// 	public I_TableMgr TblMgr{get;set;}

// 	public SqliteConnection Connection{get;set;}


// 	public async Task<Func<
// 		IEnumerable<T_Entity>
// 		,CancellationToken
// 		,Task<nil>
// 	>> Fn_InsertManyAsy(CancellationToken ct){
// 		var Tbl = TblMgr.GetTable<T_Entity>();
// 		var Cmd = Connection.CreateCommand();
// 		Cmd.CommandText = $"SELECT * FROM {Tbl.Name}";

// 		var Fn = async(
// 			IEnumerable<T_Entity> Entitys
// 			,CancellationToken ct
// 		)=>{

// 			return Nil;
// 		};
// 		return Fn;
// 	}

// 	public async Task<Func<
// 		T_Id2
// 		,T_Entity
// 		,CancellationToken
// 		,Task<T_Entity?>
// 	>> Fn_SeekByIdAsy<T_Id2>(CancellationToken ct){
// 		var Tbl = TblMgr.GetTable<T_Entity>();
// 		var Cmd = Connection.CreateCommand();
// 		Cmd.CommandText = $"SELECT * FROM {Tbl.Name} WHERE ${nameof(I_Id<nil>.Id)} = ?";

// 		var Fn = async(
// 			T_Id2 Id
// 			,T_Entity Out
// 			,CancellationToken ct
// 		)=>{
// 			if(Id is not T_Id id){
// 				throw new Exception("Id is not T_Id id");
// 			}

// 			var IdCol = Tbl.Columns[nameof(I_Id<nil>.Id)];
// 			var ConvertedId = IdCol.ToDbType(Id);

// 			Cmd.Parameters.AddWithValue("", ConvertedId);

// 			using var Reader = await Cmd.ExecuteReaderAsync(ct);
// 			while(await Reader.ReadAsync(ct)){ //只執行一次?
// 				var RawDict = new Dictionary<str, object>();
// 				for(var i = 0; i < Reader.FieldCount; i++){
// 					RawDict[Reader.GetName(i)] = Reader.GetValue(i);
// 				}
// 				var CodeDict = Tbl.ToCodeDict(RawDict);
// 				DictCtx.Assign(Out, CodeDict);
// 			}
// 			return Out;
// 		};
// 		return Fn;
// 	}

// 	public async Task<Func<
// 		IEnumerable<Id_Dict<T_Id2>>
// 		,CancellationToken
// 		,Task<nil>
// 	>> Fn_UpdateManyAsy<T_Id2>(
// 		CancellationToken ct
// 	){
// 		var Fn = async(
// 			IEnumerable<Id_Dict<T_Id2>> Id_Dicts
// 			,CancellationToken ct
// 		)=>{

// 			foreach(var id_dict in Id_Dicts){
// 				var id = id_dict.Id;
// 				var dict = id_dict.Dict;
// 				var Old = await DbSet.Select(x=>x).Where(x=>x.Id.Equals(id)).FirstOrDefaultAsync(ct);
// 				if(Old == null){
// 					continue;
// 				}
// 				DictCtx.Assign(Old, dict);//源生成器實現、無反射
// 				if(Old is I_PoBase po){
// 					po.UpdatedAt = DateTimeOffset.Now.ToUnixTimeMilliseconds();
// 				}
// 			}//~for
// 			await DbCtx.SaveChangesAsync(); //TODO ?
// 			return Nil;
// 		};
// 		return Fn;
// 	}

// 	// public async Task<I_Answer<nil>> UpdateManyAsy(IEnumerable<T_Entity> EntityList){
// 	// 	I_Answer<nil> ans = new Answer<nil>();
// 	// 	IDbContextTransaction tx = null!;
// 	// 	//DbCtx.Entry
// 	// 	try{
// 	// 		tx = await DbCtx.Database.BeginTransactionAsync();

// 	// 		foreach (var newEntity in EntityList){
// 	// 			var existingEntity = DbCtx.Set<T_Entity>().Find(newEntity.Id);
// 	// 			if (existingEntity == null){
// 	// 				break;
// 	// 			}
// 	// 			foreach (var property in DbCtx.Entry(existingEntity).Properties){
// 	// 				var newValue = newEntity.GetType().GetProperty(property.Metadata.Name)?.GetValue(newEntity);
// 	// 				if (!Equals(property.CurrentValue, newValue)){
// 	// 					property.CurrentValue = newValue;
// 	// 					property.IsModified = true;
// 	// 				}
// 	// 			}
// 	// 		}

// 	// 		await DbCtx.SaveChangesAsync();
// 	// 		await tx.CommitAsync();
// 	// 		return ans.OkWith(Nil);
// 	// 	}
// 	// 	catch (Exception e){
// 	// 		ans.AddErrException(e);
// 	// 		await tx.RollbackAsync();
// 	// 	}
// 	// 	return ans;
// 	// }

// 	public async Task<Func<
// 		T_Id2
// 		,CancellationToken
// 		,Task<nil>
// 	>> Fn_DeleteOneByIdAsy<T_Id2>(){
// 		var Tbl = TblMgr.GetTable<T_Entity>();
// 		var Cmd = Connection.CreateCommand();
// 		Cmd.CommandText =
// $"DELETE FROM {Tbl.Name} WHERE ${nameof(I_Id<nil>.Id)} = ?";
// 		var Fn = async(
// 			T_Id2 Id
// 			,CancellationToken ct
// 		)=>{
// 			if(Id is not T_Id id){
// 				throw new Exception("Id is not T_Id id");
// 			}
// 			var IdCol = Tbl.Columns[nameof(I_Id<nil>.Id)];
// 			var ConvertedId = IdCol.ToDbType(Id);
// 			Cmd.Parameters.AddWithValue("", ConvertedId);
// 			using var Reader = await Cmd.ExecuteReaderAsync(ct);
// 			return Nil;
// 		};
// 		return Fn;
// 	}


// 	public async Task<Func<
// 		IEnumerable<T_Id2>
// 		,CancellationToken
// 		,Task<nil>
// 	>> Fn_DeleteManyByIdAsy<T_Id2>(){
// 		var Tbl = TblMgr.GetTable<T_Entity>();
// 		var Cmd = Connection.CreateCommand();
// 		Cmd.CommandText =
// $"DELETE FROM {Tbl.Name} WHERE ${nameof(I_Id<nil>.Id)} IN ?";
// 		var Fn = async(
// 			IEnumerable<T_Id2> Ids
// 			,CancellationToken ct
// 		)=>{
// 			// if(Id is not T_Id id){
// 			// 	throw new Exception("Id is not T_Id id");
// 			// }

// 			var IdCol = Tbl.Columns[nameof(I_Id<nil>.Id)];
// 			var ConvertedId = IdCol.ToDbType(Id);
// 			Cmd.Parameters.AddWithValue("", ConvertedId);
// 			using var Reader = await Cmd.ExecuteReaderAsync(ct);
// 			return Nil;
// 		};
// 		return Fn;
// 	}


// 	// public async Task<T_Ret> TxnAsy<T_Ret>(
// 	// 	Func<CancellationToken, Task<T_Ret>> FnAsy
// 	// 	,CancellationToken ct
// 	// ){
// 	// 	using var Tx = await DbCtx.Database.BeginTransactionAsync(ct);
// 	// 	try{
// 	// 		var ans = await FnAsy(ct);
// 	// 		await Tx.CommitAsync(ct);
// 	// 		return ans;
// 	// 	}
// 	// 	catch (System.Exception){
// 	// 		await Tx.RollbackAsync(ct);
// 	// 		throw;
// 	// 	}
// 	// }


// }
