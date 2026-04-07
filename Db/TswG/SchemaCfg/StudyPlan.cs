using Ngaq.Core.Infra.IF;
using Ngaq.Core.Shared.StudyPlan.Models;
using Ngaq.Core.Shared.StudyPlan.Models.Po.PreFilter;
using Ngaq.Core.Shared.StudyPlan.Models.Po.StudyPlan;
using Ngaq.Core.Shared.StudyPlan.Models.Po.WeightArg;
using Ngaq.Core.Shared.StudyPlan.Models.Po.WeightCalculator;
using Ngaq.Core.Shared.User.Models.Po.User;

using Ngaq.Core.Tools;
using Tsinswreng.CsSql;

namespace Ngaq.Local.Db.TswG;
public partial class LocalTblMgrIniter{
	public static ITblMgr InitStudyPlan(ITblMgr Mgr){
		var Tbl_StudyPlan = Mk<PoStudyPlan>("StudyPlan");
		Mgr.AddTbl(Tbl_StudyPlan);
		{
			var o = Tbl_StudyPlan;
			CfgPoBase(o);
			CfgBizCreateUpdateTime(o);
			o.Col(x=>x.Id).MapType(IdStudyPlan.MkTypeMapFn());
			o.Col(x=>x.Owner).MapType(MapIdUser());
			o.Col(x=>x.PreFilterId).MapType(IdPreFilter.MkTypeMapFn());
			o.Col(x=>x.WeightCalculatorId).MapType(IdWeightCalculator.MkTypeMapFn());
			o.Col(x=>x.WeightArgId).MapType(IdWeightArg.MkTypeMapFn());
			o.IdxExpr(null
				,x=>x.PreFilterId
				,x=>x.WeightCalculatorId
				,x=>x.WeightArgId
			);
			Ux_Owner_UniqName(o);
		}

		var Tbl_PreFilter = Mk<PoPreFilter>("PreFilter");
		Mgr.AddTbl(Tbl_PreFilter);
		{
			var o = Tbl_PreFilter;
			CfgPoBase(o);
			CfgBizCreateUpdateTime(o);
			o.Col(x=>x.Id).MapType(IdPreFilter.MkTypeMapFn());
			o.Col(x=>x.Owner).MapType(MapIdUser());
			o.Col(x=>x.Type).MapEnumToStr<EPreFilterType>();
			o.Col(x=>x.DataSchemaVer).MapType(MapVersion());
			Ux_Owner_UniqName(o);
		}

		var Tbl_WeightCalculator = Mk<PoWeightCalculator>("WeightCalculator");
		Mgr.AddTbl(Tbl_WeightCalculator);
		{
			var o = Tbl_WeightCalculator;
			CfgPoBase(o);
			o.Col(x=>x.Id).MapType(IdWeightCalculator.MkTypeMapFn());
			o.Col(x=>x.Owner).MapType(MapIdUser());
			o.Col(x=>x.Type).MapEnumToStr<EWeightCalculatorType>();
			Ux_Owner_UniqName(o);
		}

		var Tbl_WeightArg = Mk<PoWeightArg>("WeightArg");
		Mgr.AddTbl(Tbl_WeightArg);
		{
			var o = Tbl_WeightArg;
			CfgPoBase(o);
			CfgBizCreateUpdateTime(o);
			o.Col(x=>x.Id).MapType(IdWeightArg.MkTypeMapFn());
			o.Col(x=>x.Owner).MapType(MapIdUser());
			o.Col(x=>x.Type).MapEnumToStr<EWeightArgType>();
			o.Col(x=>x.WeightCalculatorId).MapType(IdWeightCalculator.MkTypeMapFn());
			Ux_Owner_UniqName(o);
		}

		Mgr.AddAgg(
			AggReg<JnStudyPlan, PoStudyPlan, IdStudyPlan>.Mk(
				Tbl_StudyPlan.Tbl
				,x=>x.Id
				,(root, qry)=>new JnStudyPlan{
					StudyPlan = root
					,PreFilter = root.PreFilterId.IsNullOrDefault()
						? null
						: qry.GetOne<PoPreFilter, IdPreFilter>(root.PreFilterId)
					,WeightCalculator = root.WeightCalculatorId.IsNullOrDefault()
						? null
						: qry.GetOne<PoWeightCalculator, IdWeightCalculator>(root.WeightCalculatorId)
					,WeightArg = root.WeightArgId.IsNullOrDefault()
						? null
						: qry.GetOne<PoWeightArg, IdWeightArg>(root.WeightArgId)
				}
			)
		);

		return Mgr;

	}
}
