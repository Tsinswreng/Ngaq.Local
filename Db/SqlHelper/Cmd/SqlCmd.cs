using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;

namespace Tsinswreng.SqlHelper.Cmd;


public class SqliteCmd: I_SqlCmd{
	public SqliteCommand DbCmd{get;set;}
	public str? Sql{get;set;}
	public SqliteCmd(SqliteCommand DbCmd){
		this.DbCmd = DbCmd;
	}


	public I_SqlCmd WithCtx(I_DbFnCtx? Ctx){
		if(Ctx?.Txn?.RawTxn != null){
			DbCmd.Transaction = (SqliteTransaction)Ctx.Txn.RawTxn;
		}
		return this;
	}

/// <summary>
/// @名稱 佔位
/// </summary>
/// <param name="Params"></param>
/// <returns></returns>
	public I_SqlCmd Args(IDictionary<str, object> Params){
		DbCmd.Parameters.Clear();//不清空舊參數 續ˣ珩DbCmd蜮報錯
		foreach(var (k,v) in Params){
			DbCmd.Parameters.AddWithValue("@"+k, CodeValToDbVal(v));
		}
		return this;
	}

/// <summary>
/// 「?」 佔位
/// </summary>
/// <param name="Params"></param>
/// <returns></returns>
	public I_SqlCmd Args(IEnumerable<object> Params){
		DbCmd.Parameters.Clear();
		var i = 1;
		foreach(var v in Params){
			DbCmd.Parameters.AddWithValue("@"+i, CodeValToDbVal(v)); //這対嗎?
		}
		return this;
	}

/// <summary>
/// 若含null則做DBNull與null之轉、否則原樣返
/// </summary>
/// <param name="DbVal"></param>
/// <returns></returns>
	public object DbValToCodeVal(object DbVal){
		if(DbVal is DBNull){
			return null!;
		}
		return DbVal;
	}

	public object CodeValToDbVal(object CodeVal){
		if(CodeVal == null){
			return DBNull.Value;
		}
		return CodeVal;
	}

	public async IAsyncEnumerable<IDictionary<str, object>> RunAsy(
		[EnumeratorCancellation]
		CancellationToken ct
	){
		using var Reader = await DbCmd.ExecuteReaderAsync(ct);
		while(await Reader.ReadAsync(ct)){
			var RawDict = new Dictionary<str, object>();
			for(int i = 0; i < Reader.FieldCount; i++){
				RawDict.Add(Reader.GetName(i), DbValToCodeVal(Reader.GetValue(i)));
			}
			yield return RawDict;
		}
	}
}
