using System.Linq.Expressions;
using Ngaq.Core.Infra;
using Ngaq.Core.Model.Bo;
using Ngaq.Core.Model.Po;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Model.UserCtx;
using Ngaq.Core.Tools;
using Ngaq.Local.Db;
using Tsinswreng.SqlHelper;
using Tsinswreng.SqlHelper.Cmd;

namespace Ngaq.Local.Dao;


public class DaoSql_Word(
		ISqlCmdMkr SqlCmdMkr
		,ITableMgr TblMgr
		,RepoSql<Po_Word, IdWord> RepoWord
		,RepoSql<Po_Kv, Core.Model.Po.Kv.IdKv> RepoKv
		,RepoSql<Po_Learn, IdLearn> RepoLearn
){
	// public RepoSql<Po_Word, IdWord> RepoWord{get;set;}
	// public RepoSql<Po_Kv, IdKv> RepoKv{get;set;}
	// public RepoSql<Po_Learn, IdKv> RepoLearn{get;set;}

	// public ISqlCmdMkr SqlCmdMkr{get;set;}
	// public ITableMgr TblMgr{get;set;}

	public async Task<Func<
		I_UserCtx
		,str
		,str
		,CancellationToken
		,Task<IdWord?>
	>>
	Fn_SelectIdByFormIdEtLangAsy(
		I_DbFnCtx Ctx
		,CancellationToken ct
	){
		var T = TblMgr.GetTable<Po_Word>();
		var F = TblMgr.SqlMkr;
		var Sql =
$"""
SELECT {T.Field(nameof(IHasId<nil>.Id))} FROM {T.Quote(T.Name)}
WHERE {T.Field(nameof(Po_Word.Owner))} = {F.Param(nameof(Po_Word.Owner))}
AND {T.Field(nameof(Po_Word.WordFormId))} = {F.Param(nameof(Po_Word.WordFormId))}
AND {T.Field(nameof(Po_Word.Lang))} = {F.Param(nameof(Po_Word.Lang))}
""";
		var SqlCmd = await SqlCmdMkr.PrepareAsy(Ctx, Sql, ct);

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
				[nameof(Po_Word.Owner)] = UserId
				,[nameof(Po_Word.WordFormId)] = FormId
				,[nameof(Po_Word.Lang)] = Lang
			};
//TODO 檢查有無漏T.ToDbDict(Params)者;  多表聯合查詢旹 以此構建參數dict不好用
			var GotDict = await SqlCmd.Args(T.ToDbDict(Params)).RunAsy(ct).FirstOrDefaultAsync(ct);
			if(GotDict == null){
				return null;
			}
			var ans = GotDict[T.ToDbName(nameof(IHasId<nil>.Id))];
			return new IdWord(ToolId.ByteArrToUInt128((u8[])ans));
		};
	}


/// <summary>
///
/// </summary>
/// <param name="ct"></param>
/// <returns></returns>
	public async Task<Func<
		IdWord
		,CancellationToken
		,Task<Bo_Word?>
	>> Fn_SelectBoWordByIdAsy(
		I_DbFnCtx? Ctx
		,CancellationToken ct
	){
		var TW = TblMgr.GetTable<Po_Word>();
		var TK = TblMgr.GetTable<Po_Kv>();
		var TL = TblMgr.GetTable<Po_Learn>();
		var Sql_SeekByFKey = (str TblName)=>{
			var Sql =
$"""
SELECT * FROM {TblName}
WHERE {TK.Field(nameof(Po_Kv.FKey_UInt128))} = {TW.Param(nameof(Po_Kv.FKey_UInt128))}
""";
			return Sql;
		};
		var GetPoWordById = await RepoWord.Fn_SeekByIdAsy<IdWord>(Ctx, ct);
		var Cmd_SeekKv = await SqlCmdMkr.PrepareAsy(Ctx, Sql_SeekByFKey(TK.Quote(TK.Name)), ct);
		var Cmd_SeekLearn = await SqlCmdMkr.PrepareAsy(Ctx, Sql_SeekByFKey(TL.Quote(TL.Name)), ct);

		var Fn = async(
			IdWord Id
			,CancellationToken ct
		)=>{
			var Po_Word = await GetPoWordById(Id, ct);
			if(Po_Word == null){
				return null;
			}
			var Arg = new Dictionary<str, object>{
				[nameof(Po_Kv.FKey_UInt128)] = Id.Value
			};
			var RawPropDicts = await Cmd_SeekKv.Args(TK.ToDbDict(Arg)).RunAsy(ct)
				.Select(dbDict=>TK.DbDictToPo<Po_Kv>(dbDict))
				.ToListAsync(ct)
			;
			var RawLearnDicts = await Cmd_SeekLearn.Args(TL.ToDbDict(Arg)).RunAsy(ct)
				.Select(dbDict=>TL.DbDictToPo<Po_Learn>(dbDict))
				.ToListAsync(ct)
			;
			var ans = new Bo_Word{
				Po_Word = Po_Word
				,Props = RawPropDicts
				,Learns = RawLearnDicts
			};
			return ans;
		};
		return Fn;
	}



	public async Task<Func<
		IEnumerable<Bo_Word>
		,CancellationToken
		,Task<nil>
	>> Fn_InsertBoWordsAsy(
		I_DbFnCtx? Ctx
		,CancellationToken ct
	) {
		var InsertPoWords = await RepoWord.Fn_InsertManyAsy(Ctx, ct);
		var InsertPoKvs = await RepoKv.Fn_InsertManyAsy(Ctx, ct);
		var InsertPoLearns = await RepoLearn.Fn_InsertManyAsy(Ctx, ct);

		var Fn = async(
			IEnumerable<Bo_Word> Bo_Words
			,CancellationToken ct
		)=>{
			u64 BatchSize = 0xfff;
			using var Po_Words = new BatchListAsy<Po_Word, nil>(async(list, ct)=>{
				await InsertPoWords(list,ct);
				return Nil;
			}, BatchSize);
			using var Po_Kvs = new BatchListAsy<Po_Kv, nil>(async(e, ct)=>{
				await InsertPoKvs(e, ct);
				return Nil;
			}, BatchSize);
			using var Po_Learns = new BatchListAsy<Po_Learn, nil>(async(e, ct)=>{
				await InsertPoLearns(e, ct);
				return Nil;
			}, BatchSize);
			u64 i = 0;
			foreach (var Bo_Word in Bo_Words) {
				await Po_Words.AddAsy(Bo_Word.Po_Word, ct);
				foreach (var Prop in Bo_Word.Props) {
					await Po_Kvs.AddAsy(Prop, ct);
				}
				foreach (var Learn in Bo_Word.Learns) {
					await Po_Learns.AddAsy(Learn, ct);
				}
				i++;
			}
			await Po_Words.EndAsy(ct);
			await Po_Kvs.EndAsy(ct);
			await Po_Learns.EndAsy(ct);
			return Nil;
		};
		return Fn;
	}




	public async Task<Func<
		IEnumerable<Po_Kv>
		,CancellationToken
		,Task<nil>
	>> Fn_InsertPoKvsAsy(
		I_DbFnCtx? Ctx
		,CancellationToken ct
	){
		var InsertMany = await RepoKv.Fn_InsertManyAsy(Ctx, ct);
		var Fn = async(
			IEnumerable<Po_Kv> Po_Kvs
			,CancellationToken ct
		)=>{
			await InsertMany(Po_Kvs, ct);
			return Nil;
		};
		return Fn;
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

