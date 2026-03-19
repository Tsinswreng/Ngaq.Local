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


}
