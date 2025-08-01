namespace Ngaq.Local.Db.TswG;
using Tsinswreng.CsSqlHelper;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Infra;
using Ngaq.Core.Model.Po;
using System.Data;
using Ngaq.Core.Model.Sys.Po.User;
using Ngaq.Core.Models.Po;
using Tsinswreng.CsUlid;
using Ngaq.Core.Word.Models.Po.Learn;
using Tsinswreng.CsTools;

//using Id_User = Ngaq.Core.Model.Po.User.IdUser;
//using Id_Word = Ngaq.Core.Model.Po.Word.IdWord;
//using Id_Kv = Ngaq.Core.Model.Po.Kv.IdKv;

public  partial class LocalTblMgrIniter{
	protected ITblMgr Mgr;
	public LocalTblMgrIniter(ITblMgr Mgr){
		this.Mgr = Mgr;
	}

	static IDbTypeConvFns<i64,Tempus> MapTempus(){
		return DbTypeConvFns<i64, Tempus>.Mk(
			tempus=>tempus.Value,
			val=>new Tempus(val)
		);
	}

	static IDbTypeConvFns<i64?,Tempus?> MapTempusN(){
		return DbTypeConvFns<i64?, Tempus?>.Mk(
			tempus=>tempus?.Value,
			val=>val==null?null:new Tempus(val.Value)
		);
	}

	static IDbTypeConvFns<u8[], IdUser> MapIdUser(){
		return DbTypeConvFns<u8[], IdUser>.Mk(
			(id)=>id.Value.ToByteArr(),
			(val)=>IdUser.FromByteArr(val)
		);
	}

	static IDbTypeConvFns<u8[]?, IdUser?> MapIdUserN(){
		return DbTypeConvFns<u8[]?, IdUser?>.Mk(
			(id)=>id?.Value.ToByteArr(),
			(val)=>val==null?null:IdUser.FromByteArr(val)
		);
	}

	static IDbTypeConvFns<u8[], IdWord> MapIdWord(){
		return DbTypeConvFns<u8[], IdWord>.Mk(
			(id)=>id.Value.ToByteArr(),
			(val)=>IdWord.FromByteArr(val)
		);
	}


	protected bool _Inited{get;set;} = false;

	protected nil CfgBizTimeVer(ITable Tbl){
		var o = Tbl;
		o.SetCol(nameof(I_BizTimeVer.BizTimeVer)).HasConversionEtMapType(MapTempus());
		return NIL;
	}



	protected nil CfgPoBase<TPo>(ITable Tbl){
		var o = Tbl;
		o.CodeIdName = nameof(I_Id<nil>.Id);
		o.SetCol(nameof(I_Id<nil>.Id)).AdditionalSqls(["PRIMARY KEY"]);

		o.SetCol(nameof(IPoBase.CreatedAt)).HasConversionEtMapType(MapTempus());
		o.SetCol(nameof(IPoBase.DbCreatedAt)).HasConversionEtMapType(MapTempus());
		o.SetCol(nameof(IPoBase.UpdatedAt)).HasConversionEtMapType(MapTempusN());
		o.SetCol(nameof(IPoBase.DbUpdatedAt)).HasConversionEtMapType(MapTempusN());

		o.SetCol(nameof(IPoBase.Status)).HasConversionEtMapType<i32?, PoStatus>(
			s=>Convert.ToInt32(s.Value),
			val=>new PoStatus(Convert.ToInt32(val))
			,ObjToRaw: (obj)=>Convert.ToInt32(obj)
		);

		o.SetCol(nameof(IPoBase.CreatedBy)).HasConversionEtMapType(MapIdUserN());
		o.SetCol(nameof(IPoBase.LastUpdatedBy)).HasConversionEtMapType(MapIdUserN());

		o.SoftDelCol = new SoftDelol{
			CodeColName = nameof(IPoBase.Status)
			,FnDelete = (statusObj)=>{
				return PoStatus.Deleted.Value;
			}
			,FnRestore = (statusObj)=>{
				return PoStatus.Normal.Value;
			}
		};
		return NIL;
	}

	// protected ITable Mk<T>(str DbTblName){
	// 	var TypeDict = CoreDictMapper.Inst.GetTypeDictShallowT<T>();
	// 	return Table.Mk(
	// 		CoreDictMapper.Inst
	// 		,DbTblName
	// 		,TypeDict
	// 	);
	// }

	protected ITable Mk<T>(str DbTblName){
		return Table.FnMkTbl<T>(CoreDictMapper.Inst)(DbTblName);
	}



	protected ITable CfgI_WordId<TPo>(ITable Tbl){
		var o = Tbl;
		o.SetCol(nameof(PoWordProp.WordId)).HasConversionEtMapType(MapIdWord());
		return o;
	}

	public nil Init(){
		Mgr.AddTable(new SchemaHistoryTblMkr().MkTbl());

		var Tbl_Word = Mk<PoWord>("Word");
		Mgr.AddTable(Tbl_Word);
		{
			var o = Tbl_Word;
			CfgPoBase<PoWord>(o);
			CfgBizTimeVer(o);
			o.CodeIdName = nameof(PoWord.Id);
			o.SetCol(nameof(PoWord.Id)).HasConversionEtMapType(MapIdWord());
			o.SetCol(nameof(PoWord.Owner)).HasConversionEtMapType(MapIdUser());
			o.InnerAdditionalSqls.AddRange([
$"UNIQUE({o.Fld(nameof(PoWord.Owner))}, {o.Fld(nameof(PoWord.Head))}, {o.Fld(nameof(PoWord.Lang))})"
			]);
			o.OuterAdditionalSqls.AddRange([
$"CREATE INDEX {o.Qt("Idx_Word_Head_Lang")} ON {o.Qt(o.DbTblName)}({o.Fld(nameof(PoWord.Head))}, {o.Fld(nameof(PoWord.Lang))})"
,$"CREATE INDEX {o.Qt("Idx_Word_CreatedAt")} ON {o.Qt(o.DbTblName)}({o.Fld(nameof(PoWord.CreatedAt))})"
,$"CREATE INDEX {o.Qt("Idx_Word_UpdatedAt")} ON {o.Qt(o.DbTblName)}({o.Fld(nameof(PoWord.UpdatedAt))})"
			]);
		}

		var Tbl_Prop = Mk<PoWordProp>("WordProp");
		Mgr.AddTable(Tbl_Prop);
		{
			var o = Tbl_Prop;
			CfgPoBase<PoWordProp>(o);
			CfgI_WordId<PoWordProp>(o);
			o.CodeIdName = nameof(PoWordProp.Id);
			o.SetCol(nameof(PoWordProp.Id)).HasConversionEtMapType<u8[], IdWordProp>(
				(id)=>id.Value.ToByteArr(),
				(val)=>IdWordProp.FromByteArr(val)
			);
			o.OuterAdditionalSqls.AddRange([
$"CREATE INDEX {o.Qt("IdxKStr")} ON {o.Qt(o.DbTblName)} ({o.Fld(nameof(PoWordProp.KStr))})"
,$"CREATE INDEX {o.Qt("IdxKI64")} ON {o.Qt(o.DbTblName)} ({o.Fld(nameof(PoWordProp.KI64))})"
			]);
		}

		var Tbl_Learn = Mk<PoWordLearn>("WordLearn");
		Mgr.AddTable(Tbl_Learn);
		{
			var o = Tbl_Learn;
			CfgPoBase<PoWordLearn>(o);
			CfgI_WordId<PoWordLearn>(o);
			o.CodeIdName = nameof(PoWordLearn.Id);
			o.SetCol(nameof(PoWordLearn.Id)).HasConversionEtMapType<u8[], IdLearn>(
				(id)=>id.Value.ToByteArr(),
				(val)=>IdLearn.FromByteArr(val)
			);
		}
		_Inited = true;
		return NIL;

	}

	// public static str GenSql<T_Po>()where T_Po:new(){
	// 	var Table = TableMgr.Inst.GetTable<T_Po>();
	// 	var name = Table.Name;
	// 	var sql = $"SELECT * FROM {name}";
	// 	var po = new T_Po();
	// 	var dict = DictCtx.ToDict(po);

	// 	foreach(var kv in dict){
	// 		System.Console.WriteLine(kv.Key);
	// 		System.Console.WriteLine(kv.Value);
	// 	}
	// 	return sql;
	// }
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


#if false
Po_Word	Id_Word
Po_Kv	Id_Kv
Po_Learn	Id_Kv
#endif
