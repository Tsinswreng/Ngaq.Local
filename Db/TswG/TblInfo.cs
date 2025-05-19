namespace Ngaq.Db;
using Tsinswreng.SqlHelper;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Infra;
using Ngaq.Core.Tools;
using Ngaq.Core.Model.Po.User;
using Ngaq.Core.Model.Po;
using System.Data;
using Microsoft.Data.Sqlite;

public class AppTblInfo{

	protected static AppTblInfo? _Inst = null;
	public static AppTblInfo Inst => _Inst??= new AppTblInfo();

	public str DbPath{get;} = "E:/_code/CsNgaq/Ngaq.Sqlite";
	public IDbConnection DbConnection{get;set;}
	public AppTblInfo(){
		DbConnection = new SqliteConnection($"Data Source={DbPath}");
		DbConnection.Open();
	}

	static AppTblInfo(){
		Inst.Init();
	}


	nil CfgPoBase<T_Po>(I_Table Tbl){
		var o = Tbl;
		o.SetCol(nameof(I_PoBase.CreatedBy)).HasConversion(
			(id)=>{return id==null?null:((Id_User)id).Value.ToByteArr();},
			(val)=>{return val==null?null:new Id_User(IdTool.ByteArrToUInt128((u8[])val));}
		);
		o.SetCol(nameof(I_PoBase.LastUpdatedBy)).HasConversion(
			(id)=>{return id==null?null:id==null?null:((Id_User)id).Value.ToByteArr();},
			(val)=>{return val==null?null:val==null?null:new Id_User(IdTool.ByteArrToUInt128((u8[])val));}
		);
		return Nil;
	}

	public void Init(){
		I_TableMgr Mgr = AppTableMgr.Inst;
		Mgr.DbType = "Sqlite";

// var d = DictCtx.ToDict(Po_Word.Example);//t
// System.Console.WriteLine(
// 	str.Join("\n", d.Keys)
// );
		var Tbl_Word = Table.Mk("Word", Po_Word.Example);
		Mgr.AddTable<Po_Word>(Tbl_Word);
		{
			var o = Tbl_Word;
			CfgPoBase<Po_Word>(o);
			o.CodeIdName = nameof(Po_Word.Id);
			o.SetCol(nameof(Po_Word.Id)).HasConversion(
				(id)=>{return id==null?null:((Id_Word)id).Value.ToByteArr();},
				(val)=>{return val==null?null:new Id_Word(IdTool.ByteArrToUInt128((u8[])val));}
			);
			o.SetCol(nameof(Po_Word.Owner)).HasConversion(
				(id)=>{return id==null?null:((Id_User)id).Value.ToByteArr();},
				(val)=>{return val==null?null:new Id_User(IdTool.ByteArrToUInt128((u8[])val));}
			);
		}

		var Tbl_Prop = Table.Mk("Prop", Po_Kv.Example);
		Mgr.AddTable<Po_Kv>(Tbl_Prop);
		{
			var o = Tbl_Prop;
			CfgPoBase<Po_Kv>(o);
			o.CodeIdName = nameof(Po_Kv.Id);
			o.SetCol(nameof(Po_Kv.Id)).HasConversion(
				(id)=>{return id==null?null:((Id_Kv)id).Value.ToByteArr();},
				(val)=>{return val==null?null:new Id_Kv(IdTool.ByteArrToUInt128((u8[])val));}
			);
			o.SetCol(nameof(Po_Kv.FKey_UInt128)).HasConversion(
				(id)=>{return id==null?null:((UInt128)id).ToByteArr();},
				(val)=>{return val==null?null:IdTool.ByteArrToUInt128((u8[])val);}
			);
		}

		var Tbl_Learn = Table.Mk("Learn", Po_Learn.Example);
		Mgr.AddTable<Po_Learn>(Tbl_Learn);
		{
			var o = Tbl_Learn;
			CfgPoBase<Po_Learn>(o);
			o.CodeIdName = nameof(Po_Learn.Id);
			o.SetCol(nameof(Po_Learn.Id)).HasConversion(
				(id)=>{return id==null?null:((Id_Kv)id).Value.ToByteArr();},
				(val)=>{return val==null?null:new Id_Kv(IdTool.ByteArrToUInt128((u8[])val));}
			);
			o.SetCol(nameof(Po_Learn.FKey_UInt128)).HasConversion(
				(id)=>{return id==null?null:((UInt128)id).ToByteArr();},
				(val)=>{return val==null?null:IdTool.ByteArrToUInt128((u8[])val);}
			);
		}

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
