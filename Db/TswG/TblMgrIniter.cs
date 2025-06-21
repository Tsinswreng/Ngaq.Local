namespace Ngaq.Local.Db.TswG;
using Tsinswreng.CsSqlHelper;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Infra;
using Ngaq.Core.Tools;
using Ngaq.Core.Model.Po;
using System.Data;
using Microsoft.Data.Sqlite;
using Ngaq.Core.Model.Sys.Po.User;
using Ngaq.Core.Models.Po;
using Tsinswreng.CsUlid;
using ToolId = Tsinswreng.CsUlid.IdTool;
using Tsinswreng.CsCore.Tools;
using Tsinswreng.CsCore.Files;
using Ngaq.Core.Infra.Cfg;
using Tsinswreng.CsSqlHelper.Sqlite;
using Ngaq.Core.Word.Models.Po.Learn;

//using Id_User = Ngaq.Core.Model.Po.User.IdUser;
//using Id_Word = Ngaq.Core.Model.Po.Word.IdWord;
//using Id_Kv = Ngaq.Core.Model.Po.Kv.IdKv;



public class TblMgrIniter{
	protected ITblMgr Mgr;
	public TblMgrIniter(ITblMgr Mgr){
		this.Mgr = Mgr;
	}

	protected bool _Inited{get;set;} = false;

	protected nil CfgPoBase<TPo>(ITable Tbl){
		var o = Tbl;

		o.CodeColId = nameof(I_Id<nil>.Id);
		o.SetCol(nameof(I_Id<nil>.Id)).AdditionalSqls(["PRIMARY KEY"]);

		o.SetCol(nameof(IPoBase.CreatedAt)).HasConversionEtMapType<i64,Tempus>(
			tempus=>tempus.Value,
			val=>new Tempus(val)
		);
		o.SetCol(nameof(IPoBase.DbCreatedAt)).HasConversionEtMapType<i64, Tempus>(
			tempus=>tempus.Value,
			val=>new Tempus(val)
		);
		o.SetCol(nameof(IPoBase.UpdatedAt)).HasConversionEtMapType<i64?, Tempus?>(
			tempus=>tempus?.Value,
			val=>val==null?null:new Tempus(val.Value)
		);
		o.SetCol(nameof(IPoBase.DbUpdatedAt)).HasConversionEtMapType<i64?, Tempus?>(
			tempus=>tempus?.Value,
			val=>val==null?null:new Tempus(val.Value)
		);

		o.SetCol(nameof(IPoBase.Status)).HasConversionEtMapType<i32?, PoStatus>(
			s=>Convert.ToInt32(s.Value),
			val=>new PoStatus(Convert.ToInt32(val))
			,ObjToRaw: (obj)=>Convert.ToInt32(obj)
		);

		o.SetCol(nameof(IPoBase.CreatedBy)).HasConversionEtMapType<u8[]?, IdUser?>(
			(id)=>id?.Value.ToByteArr(),
			(val)=>val==null?null:IdUser.FromByteArr(val)
		);
		o.SetCol(nameof(IPoBase.LastUpdatedBy)).HasConversionEtMapType<u8[]?, IdUser?>(
			(id)=>id?.Value.ToByteArr(),
			(val)=>val==null?null:IdUser.FromByteArr(val)
		);

		o.SoftDeleteCol = new SoftDeleteCol{
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

	protected ITable Mk<T>(str Name, T Example){
		var ExDict = CoreDictMapper.Inst.GetTypeDictShallowT<T>();
		return Table.Mk(
			CoreDictMapper.Inst
			,Name
			,ExDict
		);
	}


	protected ITable CfgI_WordId<TPo>(ITable Tbl){
		var o = Tbl;
		o.SetCol(nameof(PoWordProp.WordId)).HasConversionEtMapType<u8[], IdWord>(
			(id)=>id.Value.ToByteArr(),
			(val)=>IdWord.FromByteArr(val)
		);
		return o;
	}

	public nil Init(){
		Mgr.AddTable<SchemaHistory>(new SchemaHistoryTblMkr().MkTbl());

		var Tbl_Word = Mk("Word", PoWord.Example);
		Mgr.AddTable<PoWord>(Tbl_Word);
		{
			var o = Tbl_Word;
			CfgPoBase<PoWord>(o);
			o.CodeColId = nameof(PoWord.Id);
			o.SetCol(nameof(PoWord.Id)).HasConversionEtMapType<u8[], IdWord>(
				(id)=>id.Value.ToByteArr(),
				(val)=>IdWord.FromByteArr(val)
			);
			o.SetCol(nameof(PoWord.Owner)).HasConversionEtMapType<u8[], IdUser>(
				(id)=>id.Value.ToByteArr(),
				(val)=>IdUser.FromByteArr(val)
			);
			o.InnerAdditionalSqls.AddRange([
$"UNIQUE({o.Field(nameof(PoWord.Owner))}, {o.Field(nameof(PoWord.Head))}, {o.Field(nameof(PoWord.Lang))})"
			]);
			o.OuterAdditionalSqls.AddRange([
$"CREATE INDEX {o.Quote("Idx_Word_Head_Lang")} ON {o.Quote(o.Name)}({o.Field(nameof(PoWord.Head))}, {o.Field(nameof(PoWord.Lang))})"
,$"CREATE INDEX {o.Quote("Idx_Word_CreatedAt")} ON {o.Quote(o.Name)}({o.Field(nameof(PoWord.CreatedAt))})"
,$"CREATE INDEX {o.Quote("Idx_Word_UpdatedAt")} ON {o.Quote(o.Name)}({o.Field(nameof(PoWord.UpdatedAt))})"
			]);
		}

		var Tbl_Prop = Mk("WordProp", PoWordProp.Example);
		Mgr.AddTable<PoWordProp>(Tbl_Prop);
		{
			var o = Tbl_Prop;
			CfgPoBase<PoWordProp>(o);
			CfgI_WordId<PoWordProp>(o);
			o.CodeColId = nameof(PoWordProp.Id);
			o.SetCol(nameof(PoWordProp.Id)).HasConversionEtMapType<u8[], IdWordProp>(
				(id)=>id.Value.ToByteArr(),
				(val)=>IdWordProp.FromByteArr(val)
			);
			o.OuterAdditionalSqls.AddRange([
$"CREATE INDEX {o.Quote("IdxKStr")} ON {o.Quote(o.Name)} ({o.Field(nameof(PoWordProp.KStr))})"
,$"CREATE INDEX {o.Quote("IdxKI64")} ON {o.Quote(o.Name)} ({o.Field(nameof(PoWordProp.KI64))})"
			]);
		}

		var Tbl_Learn = Mk("WordLearn", PoWordLearn.Example);
		Mgr.AddTable<PoWordLearn>(Tbl_Learn);
		{
			var o = Tbl_Learn;
			CfgPoBase<PoWordLearn>(o);
			CfgI_WordId<PoWordLearn>(o);
			o.CodeColId = nameof(PoWordLearn.Id);
			o.SetCol(nameof(PoWordLearn.Id)).HasConversionEtMapType<u8[], IdLearn>(
				(id)=>id.Value.ToByteArr(),
				(val)=>IdLearn.FromByteArr(val)
			);
			o.SetCol(nameof(PoWordLearn.WordId)).HasConversionEtMapType<u8[], IdWord>(
				(id)=>id.Value.ToByteArr(),
				(val)=>IdWord.FromByteArr(val)
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
