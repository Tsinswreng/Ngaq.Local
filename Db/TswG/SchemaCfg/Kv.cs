using Ngaq.Core.Infra.IF;
using Ngaq.Core.Shared.Kv.Models;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Sys.Models;
using Tsinswreng.CsSql;

namespace Ngaq.Local.Db.TswG;
public partial class LocalTblMgrIniter{
	public static ITblMgr InitKv(ITblMgr Mgr){
		var Tbl_Cfg = Mk<PoKv>("Kv");
		Mgr.AddTbl(Tbl_Cfg.Tbl);
		{
			var o = Tbl_Cfg;
			CfgPoBase(o);
			CfgIPoKv(o);
			o.Col(x=>x.Id).MapType(IdKv.MkTypeMapFn());
			o.Col(x=>x.Owner).MapType(IdUser.MkTypeMapFn());

			o.IdxExpr(null, x=>x.Owner);
			
			var optUnique1 = new OptMkIdx{Unique = true, Where = o.Tbl.SqlIsNonDel() + $" AND {o.Tbl.QtCol(nameof(PoKv.KType))} = '{o.Tbl.UpperToRaw(EKvType.Str, nameof(PoKv.KType))}'"};
			o.Idx(optUnique1, [nameof(PoKv.Owner), nameof(PoKv.KStr)]);

			var optUnique2 = new OptMkIdx{Unique = true, Where = o.Tbl.SqlIsNonDel() + $" AND {o.Tbl.QtCol(nameof(PoKv.KType))} = '{o.Tbl.UpperToRaw(EKvType.I64, nameof(PoKv.KType))}'"};
			o.Idx(optUnique2, [nameof(PoKv.Owner), nameof(PoKv.KI64)]);
		}
		return Mgr;
	}
}
