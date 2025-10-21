namespace Ngaq.Local.Db.TswG;
using Tsinswreng.CsSqlHelper;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Infra;
using Ngaq.Core.Model.Po;
using System.Data;
using Ngaq.Core.Models.Po;
using Tsinswreng.CsUlid;
using Ngaq.Core.Word.Models.Po.Learn;
using Tsinswreng.CsTools;
using Ngaq.Core.Sys.Models;
using Ngaq.Core.Word.Models.Po.Word;
using Ngaq.Core.Model.Po.Role;
using Ngaq.Core.Models.Sys.Po.Permission;
using Ngaq.Core.Domains.User.Models.Po.User;
using Ngaq.Core.Domains.User.Models;
using Ngaq.Core.Domains.Word.Models.Po.Kv;
using Ngaq.Core.Domains.Word.Models.Learn_;
using Ngaq.Core.Domains.Base.Models.Po;
using Ngaq.Core.Domains.Kv.Models;
using Ngaq.Core.Domains.Word.Models.Po.Word;
using Ngaq.Core.Domains.User.Models.Po;

public partial class LocalTblMgrIniter{
	const str MkIdx = "CREATE INDEX"; //不建議加 "IF NOT EXISTS" 以免掩蓋錯誤
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

	protected nil CfgBizTimeVer(ITable Tbl){
		var o = Tbl;
		o.SetCol(nameof(I_BizTimeVer.BizTimeVer)).MapType(MapTempus());
		return NIL;
	}

	public ITable CfgBizCreateUpdateTime(ITable Tbl){
		var o = Tbl;
		o.SetCol(nameof(IBizCreateUpdateTime.BizCreatedAt)).MapType(MapTempus());
		o.SetCol(nameof(IBizCreateUpdateTime.BizUpdatedAt)).MapType(MapTempusN());
		return o;
	}

	public ITable CfgPoBase(ITable Tbl){
		var o = Tbl;
		o.CodeIdName = nameof(I_Id<nil>.Id);
		o.SetCol(nameof(I_Id<nil>.Id)).AdditionalSqls(["PRIMARY KEY"]);
		o.SetCol(nameof(IPoBase.DbCreatedAt)).MapType(MapTempus());
		o.SetCol(nameof(IPoBase.DbUpdatedAt)).MapType(MapTempus());
		o.SetCol(nameof(IPoBase.DelAt)).MapType(MapDelId());

		// o.SetCol(nameof(IPoBase.CreatedBy)).MapType(IdUser.MkTypeMapFnNullable());
		// o.SetCol(nameof(IPoBase.LastUpdatedBy)).MapType(IdUser.MkTypeMapFnNullable());

		o.SoftDelCol = new SoftDelol{
			CodeColName = nameof(IPoBase.DelAt)
			,FnDelete = (o)=>{
				return new IdDel().Value;
			},FnRestore = (o)=>{
				return null;
			}
		};

		return o;
	}

	protected ITable CfgIPoKv(ITable o){
		o.SetCol(nameof(IPoKv.KType)).MapEnumTypeInt32<EKvType>();
		o.SetCol(nameof(IPoKv.VType)).MapEnumTypeInt32<EKvType>();
		o.OuterAdditionalSqls.AddRange([
$"{MkIdx} {o.Qt("Idx_"+o.DbTblName+"_KStr")} ON {o.Qt(o.DbTblName)} ({o.Fld(nameof(IPoKv.KStr))})"
,$"{MkIdx} {o.Qt("Idx_"+o.DbTblName+"_KI64")} ON {o.Qt(o.DbTblName)} ({o.Fld(nameof(IPoKv.KI64))})"
		]);
		return o;
	}

	public ITable Mk<T>(str DbTblName){
		return Table.FnMkTbl<T>(CoreDictMapper.Inst)(DbTblName);
	}

	protected ITable CfgI_WordId<TPo>(ITable Tbl){
		var o = Tbl;
		o.SetCol(nameof(PoWordProp.WordId)).MapType(IdWord.MkTypeMapFn());
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

		var Tbl_Cfg = Mk<PoKv>("Kv");
		Mgr.AddTbl(Tbl_Cfg);
		{
			var o = Tbl_Cfg;
			CfgPoBase(o);
			CfgIPoKv(o);
			o.SetCol(nameof(PoKv.Id)).MapType(IdKv.MkTypeMapFn());
			o.SetCol(nameof(PoKv.Owner)).MapType(IdUser.MkTypeMapFn());

			o.OuterAdditionalSqls.AddRange([
$"""
{MkIdx} {o.Qt($"Idx_{o.DbTblName}_{nameof(PoKv.Owner)}")}
ON {o.Qt(o.DbTblName)}({o.Fld(nameof(PoKv.Owner))})
"""
,$"""
CREATE UNIQUE INDEX {o.Qt($"Ux_{o.DbTblName}_{nameof(PoKv.Owner)}_{nameof(PoKv.KStr)}")}
ON {o.Qt(o.DbTblName)}({o.Fld(nameof(PoKv.Owner))},{o.Fld(nameof(PoKv.KStr))})
WHERE {o.SqlIsNonDel()}
AND {o.Fld(nameof(PoKv.KType))} = {o.UpperToRaw(EKvType.Str)}
"""
,$"""
CREATE UNIQUE INDEX {o.Qt($"Ux_{o.DbTblName}_{nameof(PoKv.Owner)}_{nameof(PoKv.KI64)}")}
ON {o.Qt(o.DbTblName)}({o.Fld(nameof(PoKv.Owner))},{o.Fld(nameof(PoKv.KI64))})
WHERE {o.SqlIsNonDel()}
AND {o.Fld(nameof(PoKv.KType))} = {o.UpperToRaw(EKvType.I64)}
"""

			]);
		}

		var Tbl_Word = Mk<PoWord>("Word");
		Mgr.AddTbl(Tbl_Word);
		{
			var o = Tbl_Word;
			CfgPoBase(o);
			CfgBizCreateUpdateTime(o);
			o.SetCol(nameof(PoWord.Id)).MapType(IdWord.MkTypeMapFn());
			o.SetCol(nameof(PoWord.Owner)).MapType(MapIdUser());
			o.SetCol(nameof(PoWord.StoredAt)).MapType(MapTempus());
			// o.InnerAdditionalSqls.AddRange([

			// ]);
			o.OuterAdditionalSqls.AddRange([
$"{MkIdx} {o.Qt($"Idx_{o.DbTblName}_Head_Lang")} ON {o.Qt(o.DbTblName)}({o.Fld(nameof(PoWord.Head))}, {o.Fld(nameof(PoWord.Lang))})"
,$"{MkIdx} {o.Qt($"Idx_{o.DbTblName}_CreatedAt")} ON {o.Qt(o.DbTblName)}({o.Fld(nameof(PoWord.BizCreatedAt))})"
,$"{MkIdx} {o.Qt($"Idx_{o.DbTblName}_UpdatedAt")} ON {o.Qt(o.DbTblName)}({o.Fld(nameof(PoWord.BizUpdatedAt))})"
,$"{MkIdx} {o.Qt($"Idx_{o.DbTblName}_StoragedAt")} ON {o.Qt(o.DbTblName)}({o.Fld(nameof(PoWord.BizUpdatedAt))})"
,
$"""
CREATE UNIQUE INDEX {o.Qt($"Ux_{o.DbTblName}_Owner_Head_Lang")} ON {o.Qt(o.DbTblName)} (
{o.Fld(nameof(PoWord.Owner))}
,{o.Fld(nameof(PoWord.Head))}
,{o.Fld(nameof(PoWord.Lang))}
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
			o.SetCol(nameof(PoWordProp.Id)).MapType(IdWordProp.MkTypeMapFn());

		}

		var Tbl_Learn = Mk<PoWordLearn>("WordLearn");
		Mgr.AddTbl(Tbl_Learn);
		{
			var o = Tbl_Learn;
			CfgPoBase(o);
			CfgI_WordId<PoWordLearn>(o);
			CfgBizCreateUpdateTime(o);
			o.CodeIdName = nameof(PoWordLearn.Id);
			o.SetCol(nameof(PoWordLearn.Id)).MapType(IdWordLearn.MkTypeMapFn());
			o.SetCol(nameof(PoWordLearn.LearnResult)).MapEnumTypeInt32<ELearn>();
		}
		_Inited = true;
		return NIL;

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
