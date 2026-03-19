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

	public async Task<IAsyncEnumerable<PoKv?>> BatGetByOwnerEtKI64(
		IDbFnCtx Ctx
		,IAsyncEnumerable<(IdUser Owner, i64 Key)> Owner_Key
		,CT Ct
	){
		var keys = Owner_Key.Select(x=>x.Key);
		var owners = Owner_Key.Select(x=>x.Owner);
var Sql = T.SqlSplicer().Select("*").From().Where1()
.And(T.SqlIsNonDel())
.AndEq(x=>x.Owner, m=>m.Many(owners))
.AndEq(x=>x.KI64, m=>m.Many(keys));
		var dicts = SqlCmdMkr.RunDupliSql(Ctx, Sql, Ct);
		return dicts.Select(x=>T.DbDictToEntity(x));
	}
	
	public async Task<IAsyncEnumerable<PoKv?>> BatGetByOwnerEtKStr(
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
		return dicts.Select(x=>T.DbDictToEntity(x));
	}
	
}
