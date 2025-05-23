namespace Ngaq.Local.Db;

using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Ngaq.Core.Infra.Core;
using Ngaq.Core.Infra.Db;
using Ngaq.Core.Model.Po;
using Microsoft.Data.Sqlite;
using Ngaq.Core.Infra;
using Tsinswreng.SqlHelper;
using Ngaq.Core.Model;
using System.Collections;
using Tsinswreng.SqlHelper.Cmd;



//using T = Bo_Word;
public class RepoSql
<
	TEntity
	,TId
>
	where TEntity: class, IHasId<TId>, new()
	where TId : IEquatable<TId>

{

	public RepoSql(
		ITableMgr TblMgr
		,ISqlCmdMkr SqlCmdMkr
	){
		this.TblMgr = TblMgr;
		this.SqlCmdMkr = SqlCmdMkr;
	}

	public ITableMgr TblMgr{get;set;}

	public ISqlCmdMkr SqlCmdMkr{get;set;}


	public async Task<Func<
		IEnumerable<TEntity>
		,CancellationToken
		,Task<nil>
	>> Fn_InsertManyAsy(
		I_DbFnCtx? Ctx
		,CancellationToken ct
	){
		var T = TblMgr.GetTable<TEntity>();
		var Clause = T.InsertClause(T.Columns.Keys);
		var Sql =
$"INSERT INTO {T.Quote(T.Name)} {Clause}";
		var Cmd = await SqlCmdMkr.PrepareAsy(Ctx, Sql, ct);
		var Fn = async(
			IEnumerable<TEntity> Entitys
			,CancellationToken ct
		)=>{
			var i = 0;
			foreach(var entity in Entitys){
				var CodeDict = DictCtx.ToDictT(entity);
				var DbDict = T.ToDbDict(CodeDict);
				await Cmd.Args(DbDict).RunAsy(ct).FirstOrDefaultAsync(ct);
				// try{

				// }
				// catch (System.Exception e){
				// 	if(Cmd is SqliteCmd sCmd){
				// 		//sCmd.DbCmd.Parameters
				// 	}
				// 	throw;
				// }
				i++;
			}
			return Nil;
		};
		return Fn;
	}

	public async Task<Func<
		T_Id2
		,CancellationToken
		,Task<TEntity?>
	>> Fn_SeekByIdAsy<T_Id2>(
		I_DbFnCtx? Ctx
		,CancellationToken ct
	){
		var T = TblMgr.GetTable<TEntity>();
		var Sql = $"SELECT * FROM {T.Quote(T.Name)} WHERE {T.Field(nameof(IHasId<nil>.Id))} = @1" ;
		var Cmd = await SqlCmdMkr.PrepareAsy(Ctx, Sql, ct);

		var Fn = async(
			T_Id2 Id
			,CancellationToken ct
		)=>{
			if(Id is not TId id){
				throw new Exception("Id is not T_Id id");
			}
			var IdCol = T.Columns[nameof(IHasId<nil>.Id)];
			var ConvertedId = IdCol.ToDbType(Id);
			var RawDict = await Cmd
				.Args([ConvertedId])
				.RunAsy(ct).FirstOrDefaultAsync(ct)
			;
			if(RawDict == null){
				return null;
			}
			var CodeDict = T.ToCodeDict(RawDict);
			var Ans = new TEntity();
			DictCtx.AssignT(Ans, CodeDict);
			return Ans;
		};
		return Fn;
	}

	public async Task<Func<
		IEnumerable<Id_Dict<T_Id2>>
		,CancellationToken
		,Task<nil>
	>> Fn_UpdateManyAsy<T_Id2>(
		I_DbFnCtx? Ctx
		,IDictionary<str, object> ModelDict
		,CancellationToken ct
	){

		var T = TblMgr.GetTable<TEntity>();
		T.ToCodeDict(ModelDict);
		//var F = SqliteSqlMkr.Inst;
		var Clause = T.UpdateClause(ModelDict.Keys);
		var N_Id = nameof(IHasId<nil>.Id);
		var Sql =
$"UPDATE {T.Quote(T.Name)} SET ${Clause} WHERE {T.Field(N_Id)} = {T.Param(N_Id)}";

		var Cmd = await SqlCmdMkr.PrepareAsy(Ctx, Sql, ct);
		var Fn = async(
			IEnumerable<Id_Dict<T_Id2>> Id_Dicts
			,CancellationToken ct
		)=>{
			foreach(var id_dict in Id_Dicts){
				var CodeId = id_dict.Id;
				var CodeDict = id_dict.Dict;
				var DbDict = T.ToDbDict(CodeDict);
				await Cmd.Args(DbDict).RunAsy(ct).FirstOrDefaultAsync(ct);
			}//~for
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
		T_Id2
		,CancellationToken
		,Task<nil>
	>> Fn_DeleteOneByIdAsy<T_Id2>(
		I_DbFnCtx? Ctx
		,CancellationToken ct
	){
		var Tbl = TblMgr.GetTable<TEntity>();
var Sql = $"DELETE FROM {Tbl.Name} WHERE {nameof(IHasId<nil>.Id)} = ?";

		var Cmd = await SqlCmdMkr.PrepareAsy(Ctx, Sql, ct);
		async Task<nil> Fn(
			T_Id2 Id
			, CancellationToken ct
		) {
			if (Id is not TId id) {
				throw new Exception("Id is not T_Id id");
			}
			var IdCol = Tbl.Columns[nameof(IHasId<nil>.Id)];
			var ConvertedId = IdCol.ToDbType(Id);
			await Cmd.Args([ConvertedId]).RunAsy(ct).FirstOrDefaultAsync(ct);
			return Nil;
		}
		return Fn;
	}


	// public async Task<Func<
	// 	IEnumerable<T_Entity>
	// 	,i64
	// 	,CancellationToken
	// 	,Task<nil>
	// >> Fn_BatchSetUpdateAtAsy(
	// 	CancellationToken ct
	// ){
	// 	var Fn = async(
	// 		IEnumerable<T_Entity> Pos
	// 		,i64 Time
	// 		,CancellationToken ct
	// 	)=>{
	// 		foreach(var po in Pos){
	// 			if(po is not I_HasId<T_Id> IdPo){
	// 				continue;
	// 			}

	// 		}
	// 		return Nil;
	// 	};
	// 	return Fn;
	// }


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


	// public async Task<T_Ret> TxnAsy<T_Ret>(
	// 	Func<CancellationToken, Task<T_Ret>> FnAsy
	// 	,CancellationToken ct
	// ){
	// 	using var Tx = await DbCtx.Database.BeginTransactionAsync(ct);
	// 	try{
	// 		var ans = await FnAsy(ct);
	// 		await Tx.CommitAsync(ct);
	// 		return ans;
	// 	}
	// 	catch (System.Exception){
	// 		await Tx.RollbackAsync(ct);
	// 		throw;
	// 	}
	// }


}
