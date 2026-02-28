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
	public const str MkIdx = "CREATE INDEX"; //不建議加 "IF NOT EXISTS" 以免掩蓋錯誤
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

	public static nil CfgBizTimeVer(ITable Tbl){
		var o = Tbl;
		o.Col(nameof(I_BizTimeVer.BizTimeVer)).MapType(MapTempus());
		return NIL;
	}

	public static ITable CfgBizCreateUpdateTime(ITable Tbl){
		var o = Tbl;
		o.Col(nameof(IBizCreateUpdateTime.BizCreatedAt)).MapType(MapTempus());
		o.Col(nameof(IBizCreateUpdateTime.BizUpdatedAt)).MapType(MapTempusN());
		return o;
	}

	public static ITable CfgPoBase(ITable Tbl){
		var o = Tbl;
		o.CodeIdName = nameof(I_Id<nil>.Id);
		o.Col(nameof(I_Id<nil>.Id)).AdditionalSqls(["PRIMARY KEY"]);
		o.Col(nameof(IPoBase.DbCreatedAt)).MapType(MapTempus());
		o.Col(nameof(IPoBase.DbUpdatedAt)).MapType(MapTempus());
		o.Col(nameof(IPoBase.DelAt)).MapType(MapDelId());

		// o.SetCol(nameof(IPoBase.CreatedBy)).MapType(IdUser.MkTypeMapFnNullable());
		// o.SetCol(nameof(IPoBase.LastUpdatedBy)).MapType(IdUser.MkTypeMapFnNullable());

		o.SoftDelCol = new SoftDelol{
			CodeColName = nameof(IPoBase.DelAt)
			,FnDelete = (o)=>{
				return new IdDel().Value;
			},FnRestore = (o)=>{
				return null;
			},FnSqlIsDel = ()=>Tbl.Fld<IPoBase>(x=>x.DelAt)+"=0"
			,FnSqlIsNonDel = ()=>Tbl.Fld<IPoBase>(x=>x.DelAt)+"<>0"
		};

		return o;
	}

	public static ITable CfgIPoKv(ITable o){
		o.Col(nameof(IPoKv.KType)).MapEnumToInt32<EKvType>();
		o.Col(nameof(IPoKv.VType)).MapEnumToInt32<EKvType>();
		o.OuterAdditionalSqls.AddRange([
$"{MkIdx} {o.Qt("Idx_"+o.DbTblName+"_KStr")} ON {o.Qt(o.DbTblName)} ({o.Fld(nameof(IPoKv.KStr))})"
,$"{MkIdx} {o.Qt("Idx_"+o.DbTblName+"_KI64")} ON {o.Qt(o.DbTblName)} ({o.Fld(nameof(IPoKv.KI64))})"
		]);
		return o;
	}

	public static ITable<T> Mk<T>(str DbTblName){
		return Table.FnMkTbl<T>(CoreDictMapper.Inst)(DbTblName);
	}

	public static ITable CfgI_WordId<TPo>(ITable Tbl){
		var o = Tbl;
		o.Col(nameof(PoWordProp.WordId)).MapType(IdWord.MkTypeMapFn());
		o.OuterAdditionalSqls.AddRange([
$"""
{MkIdx} {o.Qt("Idx_"+o.DbTblName+"_WordId")}
ON {o.Qt(o.DbTblName)} ({o.Fld(nameof(I_WordId.WordId))})
"""
		]);
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
		var Tbl_Wc = Mk<PoWeightCalculator>("WeightCalculator");
		Mgr.AddTbl(Tbl_Wc);
		{
			var o = Tbl_Wc;
			CfgPoBase(o);
			o.Col(nameof(PoWeightCalculator.Id)).MapType(IdWeightCalculator.MkTypeMapFn());
			o.Col(nameof(PoWeightCalculator.Type)).MapEnumToStr<EWeightCalculatorType>();
			o.OuterAdditionalSqls.AddRange([
$"""
CREATE UNIQUE INDEX {o.Qt($"Ux_{o.DbTblName}_UniqueName")}
ON {o.Qt(o.DbTblName)} ({o.Fld(nameof(PoWeightCalculator.UniqueName))})
WHERE {o.SqlIsNonDel()}
AND {o.Fld(nameof(PoWeightCalculator.UniqueName))} IS NOT NULL
AND {o.Fld(nameof(PoWeightCalculator.UniqueName))} <> ''
""",
$"""
CREATE INDEX {o.Qt($"Idx_{o.DbTblName}_{nameof(PoWeightCalculator.Type)}")}
ON {o.Qt(o.DbTblName)} ({o.Fld(nameof(PoWeightCalculator.Type))})
"""
			]);
		}

		var Tbl_Wa = Mk<PoWeightArg>("WeightArg");
		Mgr.AddTbl(Tbl_Wa);
		{
			var o = Tbl_Wa;
			CfgPoBase(o);
			o.Col(nameof(PoWeightArg.Id)).MapType(IdWeightArg.MkTypeMapFn());
			o.Col(nameof(PoWeightArg.Type)).MapEnumToStr<EWeightArgType>();
			o.OuterAdditionalSqls.AddRange([
$"""
CREATE UNIQUE INDEX {o.Qt($"Ux_{o.DbTblName}_UniqueName")}
ON {o.Qt(o.DbTblName)} ({o.Fld(nameof(PoWeightArg.UniqueName))})
WHERE {o.SqlIsNonDel()}
AND {o.Fld(nameof(PoWeightArg.UniqueName))} IS NOT NULL
AND {o.Fld(nameof(PoWeightArg.UniqueName))} <> ''
""",
$"""
CREATE INDEX {o.Qt($"Idx_{o.DbTblName}_{nameof(PoWeightArg.WeightCalculatorName)}")}
ON {o.Qt(o.DbTblName)} ({o.Fld(nameof(PoWeightArg.WeightCalculatorName))})
"""
			]);
		}

		var Tbl_Sp = Mk<PoStudyPlan>("StudyPlan");
		Mgr.AddTbl(Tbl_Sp);
		{
			var o = Tbl_Sp;
			CfgPoBase(o);
			o.Col(nameof(PoStudyPlan.Id)).MapType(IdStudyPlan.MkTypeMapFn());
			o.Col(nameof(PoStudyPlan.WeightCalculatorId)).MapType(IdWeightCalculator.MkTypeMapFn());
			o.Col(nameof(PoStudyPlan.WeightArgId)).MapType(IdWeightArg.MkTypeMapFn());
			o.OuterAdditionalSqls.AddRange([
$"""
CREATE UNIQUE INDEX {o.Qt($"Ux_{o.DbTblName}_UniqueName")}
ON {o.Qt(o.DbTblName)} ({o.Fld(nameof(PoStudyPlan.UniqueName))})
WHERE {o.SqlIsNonDel()}
AND {o.Fld(nameof(PoStudyPlan.UniqueName))} IS NOT NULL
AND {o.Fld(nameof(PoStudyPlan.UniqueName))} <> ''
""",
$"""
CREATE INDEX {o.Qt($"Idx_{o.DbTblName}_{nameof(PoStudyPlan.WeightCalculatorId)}")}
ON {o.Qt(o.DbTblName)} ({o.Fld(nameof(PoStudyPlan.WeightCalculatorId))})
"""
			]);
		}

		return Mgr;
	}

	public static ITblMgr InitKv(ITblMgr Mgr){
		var Tbl_Cfg = Mk<PoKv>("Kv");
		Mgr.AddTbl(Tbl_Cfg);
		{
			var o = Tbl_Cfg;
			CfgPoBase(o);
			CfgIPoKv(o);
			o.Col(x=>x.Id).MapType(IdKv.MkTypeMapFn());
			o.Col(x=>x.Owner).MapType(IdUser.MkTypeMapFn());

			o.OuterAdditionalSqls.AddRange([
$"""
{MkIdx} {o.Qt($"Idx_{o.DbTblName}_{nameof(PoKv.Owner)}")}
ON {o.Qt(o.DbTblName)}({o.Fld(x=>x.Owner)})
"""
,$"""
CREATE UNIQUE INDEX {o.Qt($"Ux_{o.DbTblName}_{nameof(PoKv.Owner)}_{nameof(PoKv.KStr)}")}
ON {o.Qt(o.DbTblName)}({o.Fld(x=>x.Owner)},{o.Fld(x=>x.KStr)})
WHERE {o.SqlIsNonDel()}
AND {o.Fld(nameof(PoKv.KType))} = {o.UpperToRaw(EKvType.Str)}
"""
,$"""
CREATE UNIQUE INDEX {o.Qt($"Ux_{o.DbTblName}_{nameof(PoKv.Owner)}_{nameof(PoKv.KI64)}")}
ON {o.Qt(o.DbTblName)}({o.Fld(x=>x.Owner)},{o.Fld(x=>x.KI64)})
WHERE {o.SqlIsNonDel()}
AND {o.Fld(nameof(PoKv.KType))} = {o.UpperToRaw(EKvType.I64)}
"""
			]);
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
			o.OuterAdditionalSqls.AddRange([
$"{MkIdx} {o.Qt($"Idx_{o.DbTblName}_Head_Lang")} ON {o.Qt(o.DbTblName)}({o.Fld(x=>x.Head)}, {o.Fld(x=>x.Lang)})"
,$"{MkIdx} {o.Qt($"Idx_{o.DbTblName}_CreatedAt")} ON {o.Qt(o.DbTblName)}({o.Fld(x=>x.BizCreatedAt)})"
,$"{MkIdx} {o.Qt($"Idx_{o.DbTblName}_UpdatedAt")} ON {o.Qt(o.DbTblName)}({o.Fld(x=>x.BizUpdatedAt)})"
,$"{MkIdx} {o.Qt($"Idx_{o.DbTblName}_StoragedAt")} ON {o.Qt(o.DbTblName)}({o.Fld(x=>x.BizUpdatedAt)})"
,
$"""
CREATE UNIQUE INDEX {o.Qt($"Ux_{o.DbTblName}_Owner_Head_Lang")} ON {o.Qt(o.DbTblName)} (
{o.Fld(x=>x.Owner)}
,{o.Fld(x=>x.Head)}
,{o.Fld(x=>x.Lang)}
) WHERE {o.SqlIsNonDel()}
"""
			]);
		}

		var Tbl_Prop = Mk<PoWordProp>("WordProp");
		Mgr.AddTbl(Tbl_Prop);
		{
			var o = Tbl_Prop;
			CfgPoBase(o);
			CfgI_WordId<PoWordProp>(o);
			CfgIPoKv(o);
			CfgBizCreateUpdateTime(o);
			o.Col(x=>x.Id).MapType(IdWordProp.MkTypeMapFn());

		}

		var Tbl_Learn = Mk<PoWordLearn>("WordLearn");
		Mgr.AddTbl(Tbl_Learn);
		{
			var o = Tbl_Learn;
			CfgPoBase(o);
			CfgI_WordId<PoWordLearn>(o);
			CfgBizCreateUpdateTime(o);
			o.CodeIdName = nameof(PoWordLearn.Id);
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
