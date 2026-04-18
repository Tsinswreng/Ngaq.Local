using Ngaq.Core.Infra.IF;
using Ngaq.Core.Shared.Dictionary.Models;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.Word.Models.Po.UserLang;

using Tsinswreng.CsSql;

namespace Ngaq.Backend.Db.TswG;
public partial class LocalTblMgrIniter{
	public static ITblMgr InitUserLang(ITblMgr Mgr){
		var Tbl_StudyPlan = Mk<PoUserLang>("UserLang");
		Mgr.AddTbl(Tbl_StudyPlan);
		{
			var o = Tbl_StudyPlan;
			CfgPoBase(o);
			CfgBizCreateUpdateTime(o);
			o.Col(x=>x.Id).MapType(IdUserLang.MkTypeMapFn());
			o.Col(x=>x.Owner).MapType(IdUser.MkTypeMapFn());
			o.Col(x=>x.RelLangType).MapEnumToStr<ELangIdentType>();
			o.IdxExpr(
				new OptMkIdx{
					Unique = true,
					Where = o.Tbl.SqlIsNonDel()
				}
				,x=>new{x.Owner, x.UniqName}
			);
			
			//從 RelLang查詢 UniqName (主用)
			o.IdxExpr(
				null
				,x=>new{x.Owner, x.RelLang, x.RelLangType}
			);
		}
		return Mgr;
	}
}
