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
public class RepoSql<
	TEntity
	,TId
>

	where TEntity: class, I_Id<TId>, new()
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
		CancellationToken
		,Task<u64>
	>> FnCount(
		IDbFnCtx? Ctx
		,CancellationToken Ct
	){
		var T = TblMgr.GetTable<TEntity>();
		var NCnt = "Cnt";
		var Sql =
$"SELECT COUNT(*) AS {T.Quote(NCnt)} FROM {T.Quote(T.Name)}";
		var Cmd = await SqlCmdMkr.Prepare(Ctx, Sql, Ct);
		var Fn = async(
			CancellationToken Ct
		)=>{
			var CountDict = await Cmd.Run(Ct).FirstOrDefaultAsync(Ct);
			u64 R = 0;
			if (CountDict != null){
				if(CountDict.TryGetValue(NCnt, out var Cnt)){
					if(Cnt is u64 || Cnt is i64 || Cnt is u32 || Cnt is i32){
						R = Convert.ToUInt64(Cnt);
					}
				}
			}
			return R;
		};
		return Fn;
	}


	public async Task<Func<
		IEnumerable<TEntity>
		,CancellationToken
		,Task<nil>
	>> FnInsertMany(
		IDbFnCtx? Ctx
		,CancellationToken ct
	){
		var T = TblMgr.GetTable<TEntity>();
		var Clause = T.InsertClause(T.Columns.Keys);
		var Sql =
$"INSERT INTO {T.Quote(T.Name)} {Clause}";
		var Cmd = await SqlCmdMkr.Prepare(Ctx, Sql, ct);
		var Fn = async(
			IEnumerable<TEntity> Entitys
			,CancellationToken ct
		)=>{
			var i = 0;
			foreach(var entity in Entitys){
				var CodeDict = DictCtx.ToDictT(entity);
				var DbDict = T.ToDbDict(CodeDict);
				await Cmd.Args(DbDict).Run(ct).FirstOrDefaultAsync(ct);
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
	>> FnSelectById<T_Id2>(
		IDbFnCtx? Ctx
		,CancellationToken ct
	){
		var T = TblMgr.GetTable<TEntity>();
		var Sql = $"SELECT * FROM {T.Quote(T.Name)} WHERE {T.Field(nameof(I_Id<nil>.Id))} = @1" ;
		var Cmd = await SqlCmdMkr.Prepare(Ctx, Sql, ct);

		var Fn = async(
			T_Id2 Id
			,CancellationToken ct
		)=>{
			if(Id is not TId id){
				throw new Exception("Id is not T_Id id");
			}
			var IdCol = T.Columns[nameof(I_Id<nil>.Id)];
			var ConvertedId = IdCol.ToDbType(Id);
			var RawDict = await Cmd
				.Args([ConvertedId])
				.Run(ct).FirstOrDefaultAsync(ct)
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
		IEnumerable<Id_Dict<TId2>>
		,CancellationToken
		,Task<nil>
	>> FnUpdateManyById<TId2>(
		IDbFnCtx? Ctx
		,IDictionary<str, object?> ModelDict //不當有Id
		,CancellationToken ct
	){
		var T = TblMgr.GetTable<TEntity>();
		ModelDict = new Dictionary<str, object?>(ModelDict);
		var NId = T.CodeIdName;
		ModelDict.Remove(NId);
		var Clause = T.UpdateClause(ModelDict.Keys);

		var Sql =
$"UPDATE {T.Quote(T.Name)} SET ${Clause} WHERE {T.Field(NId)} = {T.Param(NId)}";

		var Cmd = await SqlCmdMkr.Prepare(Ctx, Sql, ct);
		var Fn = async(
			IEnumerable<Id_Dict<TId2>> Id_Dicts
			,CancellationToken ct
		)=>{
			foreach(var id_dict in Id_Dicts){
				var CodeId = id_dict.Id;
				var CodeDict = id_dict.Dict;
				var DbDict = T.ToDbDict(CodeDict);
				await Cmd.Args(DbDict).Run(ct).FirstOrDefaultAsync(ct);
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
	>> FnDeleteOneById<T_Id2>(
		IDbFnCtx? Ctx
		,CancellationToken ct
	){
		var Tbl = TblMgr.GetTable<TEntity>();
var Sql = $"DELETE FROM {Tbl.Name} WHERE {nameof(I_Id<nil>.Id)} = ?";

		var Cmd = await SqlCmdMkr.Prepare(Ctx, Sql, ct);
		async Task<nil> Fn(
			T_Id2 Id
			, CancellationToken ct
		) {
			if (Id is not TId id) {
				throw new Exception("Id is not T_Id id");
			}
			var IdCol = Tbl.Columns[nameof(I_Id<nil>.Id)];
			var ConvertedId = IdCol.ToDbType(Id);
			await Cmd.Args([ConvertedId]).Run(ct).FirstOrDefaultAsync(ct);
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
