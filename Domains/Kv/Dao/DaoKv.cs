namespace Ngaq.Local.Domains.Kv.Dao;

using Ngaq.Core.Shared.Kv.Models;
using Ngaq.Core.Shared.User.Models;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.User.UserCtx;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Sys.Models;
using Ngaq.Local.Db.TswG;
using Tsinswreng.CsCfg;
using Tsinswreng.CsCore;
using Tsinswreng.CsPage;
using Tsinswreng.CsSql;
using Tsinswreng.CsTools;


using Z = DaoKv;
public partial class DaoKv(
	ISqlCmdMkr SqlCmdMkr
	,ITblMgr TblMgr
	, IRepo<PoKv, IdKv> RepoKv
){

	ISqlCmdMkr SqlCmdMkr = SqlCmdMkr;

	//ITable<PoKv> T{get=>TblMgr.GetTbl<PoKv>();}

	protected ITable<PoKv> T{
		get{return TblMgr.GetTbl<PoKv>();}
	}

	public IAsyncEnumerable<PoKv?> BatGetByOwnerEtKStr(
		IDbFnCtx Ctx
		,IAsyncEnumerable<(IdUser Owner, str Key)> Owner_Key
		,CT Ct
	){
var Sql = T.SqlSplicer().Select("*").From().Where1()
.And(T.SqlIsNonDel())
.AndEq(x=>x.Owner, m=>m.Many(Owner_Key, x=>x.Owner))
.AndEq(x=>x.KStr, m=>m.Many(Owner_Key, x=>x.Key));
		var dicts = SqlCmdMkr.RunDupliSql(Ctx, Sql, Ct);
		return dicts.Select(x=>x is null ? null : T.DbDictToEntity(x));
	}
	
	public IAsyncEnumerable<PoKv?> BatGetByOwnerEtKI64(
		IDbFnCtx Ctx
		,IAsyncEnumerable<(IdUser Owner, i64 Key)> Owner_Key
		,CT Ct
	){
var Sql = T.SqlSplicer().Select("*").From().Where1()
.And(T.SqlIsNonDel())
.AndEq(x=>x.Owner, m=>m.Many(Owner_Key, x=>x.Owner))
.AndEq(x=>x.KI64, m=>m.Many(Owner_Key, x=>x.Key));
		var dicts = SqlCmdMkr.RunDupliSql(Ctx, Sql, Ct);
		return dicts.Select(x=>x is null ? null : T.DbDictToEntity(x));
	}
	

	public IAsyncEnumerable<PoKv?> BatGetByOwnerEtKI64Wrong(
		IDbFnCtx Ctx
		,IAsyncEnumerable<(IdUser Owner, i64 Key)> Owner_Key
		,CT Ct
	){
		//會被消費兩次
		var keys = Owner_Key.Select(x=>x.Key);
		var owners = Owner_Key.Select(x=>x.Owner);
var Sql = T.SqlSplicer().Select("*").From().Where1()
.And(T.SqlIsNonDel())
.AndEq(x=>x.Owner, m=>m.Many(owners))
.AndEq(x=>x.KI64, m=>m.Many(keys));
		var dicts = SqlCmdMkr.RunDupliSql(Ctx, Sql, Ct);
		return dicts.Select(x=>x is null ? null : T.DbDictToEntity(x));
	}

	/// <summary>
	/// 示例: 用批處理保證 <paramref name="Owner_Key"/> 只被消費一遍，
	/// 避免把同一個 IAsyncEnumerable 分別 Select 成 owners/keys 後重複枚舉。
	/// </summary>
	/// <param name="Ctx">數據庫上下文。</param>
	/// <param name="Owner_Key">按位置對齊的 (Owner, Key) 參數流。</param>
	/// <param name="Ct">取消令牌。</param>
	/// <returns>與輸入位置對齊的查詢結果流。</returns>
	public IAsyncEnumerable<PoKv?> BatGetByOwnerEtKI64_DemoBatchSingleConsume(
		IDbFnCtx Ctx
		,IAsyncEnumerable<(IdUser Owner, i64 Key)> Owner_Key
		,CT Ct
	){
		// 只在「當前批次」內拆分 Owner/Key，避免對上游異步序列做二次消費。
		var Batch = new BatchCollector<(IdUser Owner, i64 Key), IAsyncEnumerable<PoKv?>>(
			async(BatchOwner_Key, Ct)=>{
				var Owners = BatchOwner_Key.Select(x=>x.Owner);
				var Keys = BatchOwner_Key.Select(x=>x.Key);
				var Sql = T.SqlSplicer().Select("*").From().Where1()
				.And(T.SqlIsNonDel())
				.AndEq(x=>x.Owner, m=>m.Many(Owners))
				.AndEq(x=>x.KI64, m=>m.Many(Keys));
				var Dicts = SqlCmdMkr.RunDupliSql(Ctx, Sql, Ct);
				var Ents = Dicts.Select(x=>x is null ? null : T.DbDictToEntity(x));
				return Ents;
			}
			,T.DbStuff.DfltOptBatch.DupliSqlBatchSize
		);
		return Batch.AllFlat(Owner_Key, Ct);
	}

	public IAsyncEnumerable<PoKv?> BatGetByOwnerEtKStrWrong(
		IDbFnCtx Ctx
		,IAsyncEnumerable<(IdUser Owner, str Key)> Owner_Key
		,CT Ct
	){
		var keys = Owner_Key.Select(x=>x.Key);
		var owners = Owner_Key.Select(x=>x.Owner);
var Sql = T.SqlSplicer().Select("*").From().Where1()
.And(T.SqlIsNonDel())
.AndEq(x=>x.Owner, m=>m.Many(owners))
.AndEq(x=>x.KStr, m=>m.Many(keys));
		var dicts = SqlCmdMkr.RunDupliSql(Ctx, Sql, Ct);
		return dicts.Select(x=>x is null ? null : T.DbDictToEntity(x));
	}
	
}
