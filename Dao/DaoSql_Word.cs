using System.Linq.Expressions;
using Ngaq.Core.Model.Po;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Model.UserCtx;
using Tsinswreng.SqlHelper;
using Tsinswreng.SqlHelper.Cmd;

namespace Ngaq.Local.Dao;


public class DaoSql_Word{

	public I_SqlCmdMkr SqlCmdMkr{get;set;}
	public I_TableMgr TblMgr{get;set;}

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
var Tbl = TblMgr.GetTable<Po_Word>();
var IdStr = Tbl.Columns[nameof(I_HasId<nil>.Id)].NameInDb;

var F = SqliteFormatter.Inst;
var Sql =
$"""
SELECT {F.Field(IdStr)} FROM {F.Field(TblMgr.GetTable<Po_Word>().Name)}
WHERE {F.Field(nameof(Po_Word.Owner))} = {F.Param(nameof(Po_Word.Owner))}
AND {F.Field(nameof(Po_Word.WordFormId))} = {F.Param(nameof(Po_Word.WordFormId))}
AND {F.Field(nameof(Po_Word.Lang))} = {F.Param(nameof(Po_Word.Lang))}
""";

		var SqlCmd = await SqlCmdMkr.PrepareAsy(Sql, ct);

/*
SELECT Id FROM Word
WHERE Owner = @Owner
AND WordFormId = @WordFormId
AND Lang = @Lang
 */
		return async (
			I_UserCtx OperatorCtx
			,str FormId
			,str Lang
			,CancellationToken ct
		)=>{
			var UserId = OperatorCtx.UserId;
			var Params = new Dictionary<str, object>{

			};//TODO
			//SqlCmd.SetParams
		};
	}


}


// public class Scalar{
// 	public static object Arg = null!;
// }

// public class Expr<T_Po>{
// 	public Expr<T_Po> Select(Expression<Func<T_Po, object>> expr){
// 		return this;
// 	}
// 	public Expr<T_Po> Where(Expression<Func<T_Po, bool>> expr){
// 		return this;
// 	}

// 	public str ToSql(){
// 		return "";
// 	}

// }



// /*

//  */

