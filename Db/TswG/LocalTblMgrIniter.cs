namespace Ngaq.Backend.Db.TswG;

using Ngaq.Core.Infra;
using Ngaq.Core.Infra.IF;
using Ngaq.Core.Shared.Base.Models.Po;
using Ngaq.Core.Shared.User.Models.Po;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Shared.Word.Models.Po.Word;
using Ngaq.Core.Word.Models.Po.Word;
using Tsinswreng.CsSql;
using Tsinswreng.CsTempus;

//direct to latest
public partial class LocalTblMgrIniter{
	public ITblMgr Mgr{get;set;}
	public LocalTblMgrIniter(ITblMgr Mgr){
		this.Mgr = Mgr;
	}

	public static IUpperTypeMapFnT<i64,UnixMs> MapTempus(){
		return UpperTypeMapFnT<i64, UnixMs>.Mk(
			raw=>new UnixMs(raw)
			,tempus=>tempus.Value
		);
	}

	public static IUpperTypeMapFnT<i64,IdDel> MapDelId(){
		return UpperTypeMapFnT<i64, IdDel>.Mk(
			raw=>new IdDel(raw)
			,tempus=>tempus.Value
		);
	}

	public static IUpperTypeMapFnT<i64?,UnixMs?> MapTempusN(){
		return UpperTypeMapFnT<i64?, UnixMs?>.Mk(
			val=>val==null?null:new UnixMs(val.Value)
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

	public static ITblSetter<T> CfgBizCreateUpdateTime<T>(ITblSetter<T> Tbl)
		where T:IBizCreateUpdateTime
	{
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
		InitNormLang(Mgr);
		InitUserLang(Mgr);
		InitNormLangToUserLang(Mgr);
		InitRecentUse(Mgr);
		_Inited = true;
		return NIL;
	}
	
	public static nil Ux_Owner_UniqName<T>(ITblSetter<T> o)
		where T: I_Owner, I_UniqName
	{
		o.IdxExpr(
			new OptMkIdx{
				Unique=true
				,Where = o.Tbl.SqlIsNonDel()
			}
			,x=>new{x.Owner, x.UniqName}
		);
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
