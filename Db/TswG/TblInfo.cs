namespace Ngaq.Db;
using Tsinswreng.SqlHelper;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Infra;
using Ngaq.Core.Tools;
using Ngaq.Core.Model.Po.User;
using Ngaq.Core.Model.Po;

public class TestTblInfo{
	static TestTblInfo(){
		Init();
	}

	// static nil CfgId<T_Id>(
	// 	I_Table Tbl
	// 	,str IdColName = "Id"
	// ){
	// 	var o = Tbl;
	// 	o.SetCol(IdColName).HasConversion(
	// 		(id)=>{return ((T_Id)id).Value.ToByteArr();},
	// 		(val)=>{return new T_Id(IdTool.ByteArrToUInt128((u8[])val));}
	// 	);
	// 	return Nil;
	// }

	static nil CfgPoBase<T_Po>(I_Table Tbl){
		var o = Tbl;
		o.SetCol(nameof(I_PoBase.CreatedBy)).HasConversion(
			(id)=>{return ((Id_User)id).Value.ToByteArr();},
			(val)=>{return new Id_User(IdTool.ByteArrToUInt128((u8[])val));}
		);
		o.SetCol(nameof(I_PoBase.LastUpdatedBy)).HasConversion(
			(id)=>{return ((Id_User)id).Value.ToByteArr();},
			(val)=>{return new Id_User(IdTool.ByteArrToUInt128((u8[])val));}
		);
		return Nil;
	}

	public static void Init(){
		I_TableMgr Mgr = TableMgr.Inst;
		var Tbl_Word = Table.Mk("Word", Po_Word.Example);
		Mgr.AddTable<Po_Word>(Tbl_Word);
		{
			var o = Tbl_Word;
			CfgPoBase<Po_Word>(o);
			o.CodeIdName = nameof(Po_Word.Id);
			o.SetCol(nameof(Po_Word.Id)).HasConversion(
				(id)=>{return ((Id_Word)id).Value.ToByteArr();},
				(val)=>{return new Id_Word(IdTool.ByteArrToUInt128((u8[])val));}
			);
			o.SetCol(nameof(Po_Word.Owner)).HasConversion(
				(id)=>{return ((Id_User)id).Value.ToByteArr();},
				(val)=>{return new Id_User(IdTool.ByteArrToUInt128((u8[])val));}
			);
		}

		var Tbl_Prop = Table.Mk("Prop", Po_Kv.Example);
		Mgr.AddTable<Po_Kv>(Tbl_Prop);
		{
			var o = Tbl_Prop;
			CfgPoBase<Po_Kv>(o);
			o.CodeIdName = nameof(Po_Kv.Id);
			o.SetCol(nameof(Po_Kv.Id)).HasConversion(
				(id)=>{return ((Id_Kv)id).Value.ToByteArr();},
				(val)=>{return new Id_Kv(IdTool.ByteArrToUInt128((u8[])val));}
			);
		}

		var Tbl_Learn = Table.Mk("Learn", Po_Learn.Example);
		Mgr.AddTable<Po_Learn>(Tbl_Learn);
		{
			var o = Tbl_Learn;
			CfgPoBase<Po_Learn>(o);
			o.CodeIdName = nameof(Po_Learn.Id);
			o.SetCol(nameof(Po_Learn.Id)).HasConversion(
				(id)=>{return ((Id_Kv)id).Value.ToByteArr();},
				(val)=>{return new Id_Kv(IdTool.ByteArrToUInt128((u8[])val));}
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
