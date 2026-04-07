using Ngaq.Core.Infra.IF;
using Ngaq.Core.Shared.RecentUse.Models.Po.RecentUse;
using Tsinswreng.CsSql;

namespace Ngaq.Local.Db.TswG;

public partial class LocalTblMgrIniter{
	public static ITblMgr InitRecentUse(ITblMgr Mgr){
		var Tbl_RecentUse = Mk<PoRecentUse>("RecentUse");
		Mgr.AddTbl(Tbl_RecentUse);
		{
			var o = Tbl_RecentUse;
			CfgPoBase(o);
			CfgBizCreateUpdateTime(o);
			o.Col(x=>x.Id).MapType(IdRecentUse.MkTypeMapFn());
			o.Col(x=>x.Owner).MapType(MapIdUser());

			// 唯一鍵：避免同一業務鍵生成多條有效記錄。
			o.IdxExpr(
				new OptMkIdx{
					Unique = true,
					Where = o.Tbl.SqlIsNonDel()
				},
				x=>new{x.Owner, x.Scene, x.EntityType, x.EntityId}
			);

			// 常見查詢：按用戶+場景拉取 recent，並按時間排序。
			o.IdxExpr(
				null,
				x=>new{x.Owner, x.Scene, x.EntityType, x.BizUpdatedAt}
			);
		}
		return Mgr;
	}
}

