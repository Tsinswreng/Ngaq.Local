namespace Ngaq.Local.Db;

using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Ngaq.Core.Infra.Core;
using Ngaq.Core.Infra.Db;
using Ngaq.Core.Model.Po;
using Microsoft.Data.Sqlite;
using Ngaq.Core.Infra;
using Tsinswreng.CsSqlHelper;
using Ngaq.Core.Model;
using System.Collections;
using Tsinswreng.CsSqlHelper.Cmd;
using Ngaq.Core.Tools;



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
		CT
		,Task<u64>
	>> FnCount(
		IDbFnCtx? Ctx
		,CT Ct
	){
		var T = TblMgr.GetTable<TEntity>();
		var NCnt = "Cnt";
		var Sql =
$"SELECT COUNT(*) AS {T.Quote(NCnt)} FROM {T.Quote(T.Name)}";
		var Cmd = await SqlCmdMkr.Prepare(Ctx, Sql, Ct);
		var Fn = async(
			CT Ct
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
		,CT
		,Task<nil>
	>> FnInsertMany(
		IDbFnCtx? Ctx
		,CT ct
	){
		var T = TblMgr.GetTable<TEntity>();
		var Clause = T.InsertClause(T.Columns.Keys);
		var Sql =
$"INSERT INTO {T.Quote(T.Name)} {Clause}";
		var Cmd = await SqlCmdMkr.Prepare(Ctx, Sql, ct);
		var Fn = async(
			IEnumerable<TEntity> Entitys
			,CT ct
		)=>{
			var i = 0;
			foreach(var entity in Entitys){
				var CodeDict = DictCtx.ToDictT(entity);
				var DbDict = T.ToDbDict(CodeDict);
				await Cmd.Args(DbDict).Run(ct).FirstOrDefaultAsync(ct);
				i++;
			}
			return NIL;
		};
		return Fn;
	}

	public async Task<Func<
		TId2
		,CT
		,Task<TEntity?>
	>> FnSelectById<TId2>(
		IDbFnCtx? Ctx
		,CT ct
	){
		var T = TblMgr.GetTable<TEntity>();
		var Params = T.MkUnnamedParam(1);
		var Sql = $"SELECT * FROM {T.Quote(T.Name)} WHERE {T.Field(nameof(I_Id<nil>.Id))} = {Params[0]}" ;
		var Cmd = await SqlCmdMkr.Prepare(Ctx, Sql, ct);

		var Fn = async(
			TId2 Id
			,CT ct
		)=>{
			if(Id is not TId id){
				throw new Exception("Id is not TId id");
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
		,CT
		,Task<nil>
	>> FnUpdateManyById<TId2>(
		IDbFnCtx? Ctx
		,IDictionary<str, object?> ModelDict //不當有Id
		,CT ct
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
			,CT ct
		)=>{
			foreach(var id_dict in Id_Dicts){
				var CodeId = id_dict.Id;
				var CodeDict = id_dict.Dict;
				var DbDict = T.ToDbDict(CodeDict);
				await Cmd.Args(DbDict).Run(ct).FirstOrDefaultAsync(ct);
			}//~for
			return NIL;
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






/// TODO 用Where Id IN (@0, @1, @2...) 㕥減次芝往返
	public async Task<Func<
		TId2
		,CT
		,Task<nil>
	>> FnDeleteOneById<TId2>(
		IDbFnCtx? Ctx
		,CT ct
	){
		var Tbl = TblMgr.GetTable<TEntity>();
var Sql = $"DELETE FROM {Tbl.Name} WHERE {nameof(I_Id<nil>.Id)} = ?";

		var Cmd = await SqlCmdMkr.Prepare(Ctx, Sql, ct);
		async Task<nil> Fn(
			TId2 Id
			, CT ct
		) {
			if (Id is not TId id) {
				throw new Exception("Id is not T_Id id");
			}
			var IdCol = Tbl.Columns[nameof(I_Id<nil>.Id)];
			var ConvertedId = IdCol.ToDbType(Id);
			await Cmd.Args([ConvertedId]).Run(ct).FirstOrDefaultAsync(ct);
			return NIL;
		}
		return Fn;
	}


//TODO TEST
	public async Task<Func<
		IEnumerable<object?>
		,CT
		,Task<nil>
	>> FnDeleteManyByKeys(
		IDbFnCtx? Ctx
		,str KeyNameInCode
		,u64 ParamNum
		,CT Ct
	){
		var T = TblMgr.GetTable<TEntity>();
		var Clause = T.NumParamClause(ParamNum);
		var Sql =
$"""
DELETE FROM {T.Quote(T.Name)} WHERE {T.Quote(KeyNameInCode)} IN ${Clause}
AND {T.Quote(KeyNameInCode)} IS NOT NULL;
""";
		var Cmd = await SqlCmdMkr.Prepare(Ctx, Sql, Ct);
		var Fn = async(
			IEnumerable<object?> Keys
			,CT Ct
		)=>{

			IList<object?> Args = new List<object?>();
			u64 i = 0, j=0;
			foreach(var key in Keys){
				Args.Add(key);
				if(j == ParamNum - 1){
					await Cmd.Args(Args).Run(Ct).FirstOrDefaultAsync(Ct);
					Args.Clear();
					j = 0;
				}
			i++;j++;}
			if(j > 0){
				await Cmd.Args(Args).Run(Ct).FirstOrDefaultAsync(Ct);
			}
			return NIL;
		};
		return Fn;
	}


	public async Task<Func<
		IEnumerable<TId2>
		,CT
		,Task<nil>
	>> FnDeleteManyByKeys<TId2>(
		IDbFnCtx? Ctx
		,str KeyNameInCode
		,u64 ParamNum
		,CT Ct
	){
		var T = TblMgr.GetTable<TEntity>();
		var NonGeneric = await FnDeleteManyByKeys(Ctx, KeyNameInCode, ParamNum, Ct);
		var Fn = async(
			IEnumerable<TId2> Ids
			,CT Ct
		)=>{
			var Args = Ids.Select(Id => T.ToDbType(KeyNameInCode, Id));
			await NonGeneric(Args, Ct);
			return NIL;
		};
		return Fn;
	}




	// public async Task<Func<
	// 	IEnumerable<T_Entity>
	// 	,i64
	// 	,CT
	// 	,Task<nil>
	// >> Fn_BatchSetUpdateAtAsy(
	// 	CT ct
	// ){
	// 	var Fn = async(
	// 		IEnumerable<T_Entity> Pos
	// 		,i64 Time
	// 		,CT ct
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
// 		,CT
// 		,Task<nil>
// 	>> Fn_DeleteManyByIdAsy<T_Id2>(){
// 		var Tbl = TblMgr.GetTable<T_Entity>();
// 		var Cmd = Connection.CreateCommand();
// 		Cmd.CommandText =
// $"DELETE FROM {Tbl.Name} WHERE ${nameof(I_Id<nil>.Id)} IN ?";
// 		var Fn = async(
// 			IEnumerable<T_Id2> Ids
// 			,CT ct
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
	// 	Func<CT, Task<T_Ret>> FnAsy
	// 	,CT ct
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
