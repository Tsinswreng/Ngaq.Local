namespace Ngaq.Local.Db.TswG;
using Ngaq.Core.Infra;
using Ngaq.Core.Model.Po;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Shared.Base.Models.Po;
using Ngaq.Core.Shared.Kv.Models;
using Ngaq.Core.Shared.StudyPlan.Models.Po.StudyPlan;
using Ngaq.Core.Shared.StudyPlan.Models.Po.WeightArg;
using Ngaq.Core.Shared.StudyPlan.Models.Po.WeightCalculator;
using Ngaq.Core.Shared.User.Models.Po;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.Word.Models.Learn_;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Shared.Word.Models.Po.Learn;
using Ngaq.Core.Shared.Word.Models.Po.Word;
using Ngaq.Core.Sys.Models;
using Ngaq.Core.Word.Models.Po.Word;
using Tsinswreng.CsCore;
using Tsinswreng.CsSqlHelper;
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
			},FnSqlIsDel = ()=>t.Fld<IPoBase>(x=>x.DelAt)+"=0"
			,FnSqlIsNonDel = ()=>t.Fld<IPoBase>(x=>x.DelAt)+"<>0"
		};

		return o;
	}

	public static ITblSetter<T> CfgIPoKv<T>(ITblSetter<T> o) where T : IPoKv {
		o.Col(nameof(IPoKv.KType)).MapEnumToInt32<EKvType>();
		o.Col(nameof(IPoKv.VType)).MapEnumToInt32<EKvType>();
		o.IdxExpr(null, x=>x.KStr, x=>x.KI64);
		return o;
	}

	[Obsolete]
	public static ITable<T> MkObslt<T>(str DbTblName){
		return Table.FnMkTbl<T>(CoreDictMapper.Inst)(DbTblName);
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
		//TODO
		return Mgr;
#if false
var Tbl_Wc = Mk<PoWeightCalculator>("WeightCalculator");
		Mgr.AddTbl(Tbl_Wc);
		{
			var o = Tbl_Wc;
			CfgPoBase(o);
			o.Col(nameof(PoWeightCalculator.Id)).MapType(IdWeightCalculator.MkTypeMapFn());
			o.Col(nameof(PoWeightCalculator.Type)).MapEnumToStr<EWeightCalculatorType>();
			o.AddIndexByCodeCols(
				$"Ux_{o.DbTblName}_UniqueName"
				, [nameof(PoWeightCalculator.UniqueName)]
				, IsUnique: true
				, WhereAnds: [
					o.SqlIsNonDel()
					, $"{o.Fld(nameof(PoWeightCalculator.UniqueName))} IS NOT NULL"
					, $"{o.Fld(nameof(PoWeightCalculator.UniqueName))} <> ''"
				]
			);
			o.AddIndexByCodeCols($"Idx_{o.DbTblName}_{nameof(PoWeightCalculator.Type)}", [nameof(PoWeightCalculator.Type)]);
		}

		var Tbl_Wa = Mk<PoWeightArg>("WeightArg");
		Mgr.AddTbl(Tbl_Wa);
		{
			var o = Tbl_Wa;
			CfgPoBase(o);
			o.Col(nameof(PoWeightArg.Id)).MapType(IdWeightArg.MkTypeMapFn());
			o.Col(nameof(PoWeightArg.Type)).MapEnumToStr<EWeightArgType>();
			o.AddIndexByCodeCols(
				$"Ux_{o.DbTblName}_UniqueName"
				, [nameof(PoWeightArg.UniqueName)]
				, IsUnique: true
				, WhereAnds: [
					o.SqlIsNonDel()
					, $"{o.Fld(nameof(PoWeightArg.UniqueName))} IS NOT NULL"
					, $"{o.Fld(nameof(PoWeightArg.UniqueName))} <> ''"
				]
			);
			o.AddIndexByCodeCols($"Idx_{o.DbTblName}_{nameof(PoWeightArg.WeightCalculatorName)}", [nameof(PoWeightArg.WeightCalculatorName)]);
		}

		var Tbl_Sp = Mk<PoStudyPlan>("StudyPlan");
		Mgr.AddTbl(Tbl_Sp);
		{
			var o = Tbl_Sp;
			CfgPoBase(o);
			o.Col(nameof(PoStudyPlan.Id)).MapType(IdStudyPlan.MkTypeMapFn());
			o.Col(nameof(PoStudyPlan.WeightCalculatorId)).MapType(IdWeightCalculator.MkTypeMapFn());
			o.Col(nameof(PoStudyPlan.WeightArgId)).MapType(IdWeightArg.MkTypeMapFn());
			o.AddIndexByCodeCols(
				$"Ux_{o.DbTblName}_UniqueName"
				, [nameof(PoStudyPlan.UniqueName)]
				, IsUnique: true
				, WhereAnds: [
					o.SqlIsNonDel()
					, $"{o.Fld(nameof(PoStudyPlan.UniqueName))} IS NOT NULL"
					, $"{o.Fld(nameof(PoStudyPlan.UniqueName))} <> ''"
				]
			);
			o.AddIndexByCodeCols($"Idx_{o.DbTblName}_{nameof(PoStudyPlan.WeightCalculatorId)}", [nameof(PoStudyPlan.WeightCalculatorId)]);
		}

		return Mgr;
#endif
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
			
			var optUnique1 = new OptMkIdx{Unique = true, Where = o.Tbl.SqlIsNonDel() + $" AND {o.Tbl.Fld(nameof(PoKv.KType))} = {o.Tbl.UpperToRaw(EKvType.Str)}"};
			o.Idx(optUnique1, [nameof(PoKv.Owner), nameof(PoKv.KStr)]);
			
			var optUnique2 = new OptMkIdx{Unique = true, Where = o.Tbl.SqlIsNonDel() + $" AND {o.Tbl.Fld(nameof(PoKv.KType))} = {o.Tbl.UpperToRaw(EKvType.I64)}"};
			o.Idx(optUnique2, [nameof(PoKv.Owner), nameof(PoKv.KI64)]);
		}
		return Mgr;
	}
	public static ITblMgr InitWord(ITblMgr Mgr){
		var Tbl_Word = Mk<PoWord>("Word");
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
			
			var optUnique = new OptMkIdx{Unique = true, Where = o.Tbl.SqlIsNonDel()};
			o.Idx(
				optUnique
				, [nameof(PoWord.Owner), nameof(PoWord.Head), nameof(PoWord.Lang)]
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
