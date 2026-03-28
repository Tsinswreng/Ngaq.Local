namespace Ngaq.Local.Db.TswG;

using Ngaq.Core.Infra;
using Ngaq.Core.Infra.IF;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Shared.Base.Models.Po;
using Ngaq.Core.Shared.Kv.Models;
using Ngaq.Core.Shared.StudyPlan.Models;
using Ngaq.Core.Shared.StudyPlan.Models.Po.PreFilter;
using Ngaq.Core.Shared.StudyPlan.Models.Po.StudyPlan;
using Ngaq.Core.Shared.StudyPlan.Models.Po.WeightArg;
using Ngaq.Core.Shared.StudyPlan.Models.Po.WeightCalculator;
using Ngaq.Core.Shared.User.Models.Po;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.Word.Models;
using Ngaq.Core.Shared.Word.Models.Learn_;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Shared.Word.Models.Po.Learn;
using Ngaq.Core.Shared.Word.Models.Po.Word;
using Ngaq.Core.Sys.Models;
using Ngaq.Core.Tools;
using Ngaq.Core.Word.Models.Po.Word;
using Tsinswreng.CsCore;
using Tsinswreng.CsSql;
using Tsinswreng.CsTools;

//direct to latest
public partial class LocalTblMgrIniter{
	public ITblMgr Mgr{get;set;}
	public LocalTblMgrIniter(ITblMgr Mgr){
		this.Mgr = Mgr;
	}

	public static IUpperTypeMapFnT<i64,Tempus> MapTempus(){
		return UpperTypeMapFnT<i64, Tempus>.Mk(
			raw=>new Tempus(raw)
			,tempus=>tempus.Value
		);
	}

	public static IUpperTypeMapFnT<i64,IdDel> MapDelId(){
		return UpperTypeMapFnT<i64, IdDel>.Mk(
			raw=>new IdDel(raw)
			,tempus=>tempus.Value
		);
	}

	public static IUpperTypeMapFnT<i64?,Tempus?> MapTempusN(){
		return UpperTypeMapFnT<i64?, Tempus?>.Mk(
			val=>val==null?null:new Tempus(val.Value)
			,tempus=>tempus?.Value
		);
	}

	public static IUpperTypeMapFnT<u8[], IdUser> MapIdUser(){
		return IdUser.MkTypeMapFn();
	}

	public static IUpperTypeMapFnT<str, Version> MapVersion(){
		return UpperTypeMapFnT<str, Version>.Mk(
			raw => Version.Parse(raw)
			,ver => ver.ToString()
		);
	}

	protected bool _Inited{get;set;} = false;

	public static ITblSetter<T> CfgBizTimeVer<T>(ITblSetter<T> Tbl){
		var o = Tbl;
		o.Col(nameof(I_BizTimeVer.BizTimeVer)).MapType(MapTempus());
		return o;
	}

	public static ITblSetter<T> CfgBizCreateUpdateTime<T>(ITblSetter<T> Tbl){
		var o = Tbl;
		o.Col(nameof(IBizCreateUpdateTime.BizCreatedAt)).MapType(MapTempus());
		o.Col(nameof(IBizCreateUpdateTime.BizUpdatedAt)).MapType(MapTempusN());
		return o;
	}

	public static ITblSetter<T> CfgPoBase<T>(ITblSetter<T> Tbl)
		where T:IPoBase, new()
	{
		var o = Tbl;
		o.Tbl.CodeIdName = nameof(I_Id<nil>.Id);
		o.Col(nameof(I_Id<nil>.Id)).AdditionalSqls(["PRIMARY KEY"]);
		o.Col(nameof(IPoBase.DbCreatedAt)).MapType(MapTempus());
		o.Col(nameof(IPoBase.DbUpdatedAt)).MapType(MapTempus());
		o.Col(nameof(IPoBase.DelAt)).MapType(MapDelId());

		// o.SetCol(nameof(IPoBase.CreatedBy)).MapType(IdUser.MkTypeMapFnNullable());
		// o.SetCol(nameof(IPoBase.LastUpdatedBy)).MapType(IdUser.MkTypeMapFnNullable());
		var t = o.Tbl;

		o.Tbl.SoftDelCol = new SoftDelol{
			CodeColName = nameof(IPoBase.DelAt)
			,FnDelete = (o)=>{
				return new IdDel().Value;
			},FnRestore = (o)=>{
				return null;
			},FnSqlIsDel = ()=>t.QtCol<IPoBase>(x=>x.DelAt)+"<>0"
			,FnSqlIsNonDel = ()=>t.QtCol<IPoBase>(x=>x.DelAt)+"=0"
		};

		return o;
	}

	public static ITblSetter<T> CfgIPoKv<T>(ITblSetter<T> o) where T : IPoKv {
		o.Col(nameof(IPoKv.KType)).MapEnumToStr<EKvType>();
		o.Col(nameof(IPoKv.VType)).MapEnumToStr<EKvType>();
		o.IdxExpr(null, x=>x.KStr, x=>x.KI64);
		return o;
	}

	public static ITblSetter<T> Mk<T>(str DbTblName){
		return Table.FnSetTbl<T>(CoreDictMapper.Inst)(DbTblName);
	}
	

	public static ITblSetter<T> CfgI_WordId<T>(ITblSetter<T> o) where T : I_WordId {
		o.Col(nameof(I_WordId.WordId)).MapType(IdWord.MkTypeMapFn());
		o.IdxExpr(null, x => x.WordId);
		return o;
	}

	public nil Init(){
		Mgr.AddTbl(new SchemaHistoryTblMkr().MkTbl());
		InitKv(Mgr);
		InitWord(Mgr);
		InitStudyPlan(Mgr);
		_Inited = true;
		return NIL;
	}

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
			o.IdxExpr(
				new OptMkIdx{
					Unique=true
					,Where = o.Tbl.SqlIsNonDel()
				}
				,x=>new{x.Owner, x.UniqName}
			);
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
			o.IdxExpr(null, x=>x.UniqName);
		}

		var Tbl_WeightCalculator = Mk<PoWeightCalculator>("WeightCalculator");
		Mgr.AddTbl(Tbl_WeightCalculator);
		{
			var o = Tbl_WeightCalculator;
			CfgPoBase(o);
			o.Col(x=>x.Id).MapType(IdWeightCalculator.MkTypeMapFn());
			o.Col(x=>x.Owner).MapType(MapIdUser());
			o.Col(x=>x.Type).MapEnumToStr<EWeightCalculatorType>();
			o.IdxExpr(
				new OptMkIdx{
					Unique=true
					,Where = o.Tbl.SqlIsNonDel()
				}
				,x=>new{x.Owner, x.UniqName}
			);
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
			o.IdxExpr(
				new OptMkIdx{
					Unique=true
					,Where = o.Tbl.SqlIsNonDel()
				}
				,x=>new{x.Owner, x.UniqName}
			);
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
			
			var optUnique1 = new OptMkIdx{Unique = true, Where = o.Tbl.SqlIsNonDel() + $" AND {o.Tbl.QtCol(nameof(PoKv.KType))} = {o.Tbl.UpperToRaw(EKvType.Str)}"};
			o.Idx(optUnique1, [nameof(PoKv.Owner), nameof(PoKv.KStr)]);
			
			var optUnique2 = new OptMkIdx{Unique = true, Where = o.Tbl.SqlIsNonDel() + $" AND {o.Tbl.QtCol(nameof(PoKv.KType))} = {o.Tbl.UpperToRaw(EKvType.I64)}"};
			o.Idx(optUnique2, [nameof(PoKv.Owner), nameof(PoKv.KI64)]);
		}
		return Mgr;
	}
	public static ITblMgr InitWord(ITblMgr Mgr){
		var Tbl_Word = Mk<PoWord>("Word");
		ExtnTblMgr.AddTbl(Mgr, Tbl_Word);
		Mgr.AddTbl(Tbl_Word);
		{
			var o = Tbl_Word;
			CfgPoBase(o);
			CfgBizCreateUpdateTime(o);
			o.Col(x=>x.Id).MapType(IdWord.MkTypeMapFn());
			o.Col(x=>x.Owner).MapType(MapIdUser());
			o.Col(x=>x.StoredAt).MapType(MapTempus());
			// o.InnerAdditionalSqls.AddRange([

			// ]);
			o.IdxExpr(
				null
				,x=>new {x.Head, x.Lang}
				,x=>x.BizCreatedAt
				,x=>x.BizUpdatedAt
				,x=>x.StoredAt
			);
			
			o.IdxExpr(
				new OptMkIdx{Unique = true, Where = o.Tbl.SqlIsNonDel()}
				,x=>new{x.Owner, x.Head, x.Lang}
			);
		}

		var Tbl_Prop = Mk<PoWordProp>("WordProp");
		Mgr.AddTbl(Tbl_Prop);
		{
			var o = Tbl_Prop;
			CfgPoBase(o);
			CfgI_WordId(o);
			CfgIPoKv(o);
			CfgBizCreateUpdateTime(o);
			o.Col(x=>x.Id).MapType(IdWordProp.MkTypeMapFn());

		}

		var Tbl_Learn = Mk<PoWordLearn>("WordLearn");
		Mgr.AddTbl(Tbl_Learn);
		{
			var o = Tbl_Learn;
			CfgPoBase(o);
			CfgI_WordId(o);
			CfgBizCreateUpdateTime(o);
			o.Col(x=>x.Id).MapType(IdWordLearn.MkTypeMapFn());
			//o.SetCol(nameof(PoWordLearn.LearnResult)).MapEnumTypeInt32<ELearn>();
			o.Col(x=>x.LearnResult).MapEnumToStr<ELearn>();
		}

		Mgr.AddAgg(
			AggReg<JnWord, PoWord, IdWord>.Mk(
				Tbl_Word.Tbl
				,x=>x.Id
				,(root, qry)=>new JnWord(
					root
					,qry.GetMany<PoWordProp, IdWord>(root.Id)
					,qry.GetMany<PoWordLearn, IdWord>(root.Id)
				)
			)
			.AddOneToMany(
				Tbl_Prop.Tbl
				,nameof(I_WordId.WordId)
				,x=>x.WordId
			)
			.AddOneToMany(
				Tbl_Learn.Tbl
				,nameof(I_WordId.WordId)
				,x=>x.WordId
			)
		);
		return Mgr;
	}

}


//取最新ʹsql腳本芝建架構
#if false
AppTblInfo.Inst.Init();
ITblMgr TblMgr = AppTableMgr.Inst;
System.Console.WriteLine(
	TblMgr.SqlMkSchema()
);
#endif


#if false
TestTblInfo.Init();
System.Console.WriteLine(
	TestTblInfo.GenSql<Po_Word>()
);
throw new Exception("AOT");
#endif
