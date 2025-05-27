namespace Ngaq.Db;
using Tsinswreng.SqlHelper;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Infra;
using Ngaq.Core.Tools;
using Ngaq.Core.Model.Po;
using System.Data;
using Microsoft.Data.Sqlite;
using Ngaq.Core.Model.Sys.Po.User;

//using Id_User = Ngaq.Core.Model.Po.User.IdUser;
//using Id_Word = Ngaq.Core.Model.Po.Word.IdWord;
//using Id_Kv = Ngaq.Core.Model.Po.Kv.IdKv;

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


	nil CfgPoBase<T_Po>(ITable Tbl){
		var o = Tbl;
		o.SetCol(nameof(IPoBase.CreatedBy)).HasConversion(
			(id)=>{return id==null?null:((IdUser)id).Value.ToByteArr();},
			(val)=>{return val==null?null:new IdUser(ToolId.ByteArrToUInt128((u8[])val));}
		);
		o.SetCol(nameof(IPoBase.LastUpdatedBy)).HasConversion(
			(id)=>{return id==null?null:id==null?null:((IdUser)id).Value.ToByteArr();},
			(val)=>{return val==null?null:val==null?null:new IdUser(ToolId.ByteArrToUInt128((u8[])val));}
		);
		return Nil;
	}

	protected ITable Mk<T>(str Name, T Example){
		var ExDict = DictCtx.ToDictT(Example);
		return Table.Mk(Name, ExDict);
	}

	public void Init(){
		ITableMgr Mgr = AppTableMgr.Inst;
		Mgr.DbType = "Sqlite";


		var Tbl_Word = Mk("Word", PoWord.Example);
		Mgr.AddTable<PoWord>(Tbl_Word);
		{
			var o = Tbl_Word;
			CfgPoBase<PoWord>(o);
			o.CodeIdName = nameof(PoWord.Id);
			o.SetCol(nameof(PoWord.Id)).HasConversion(
				(id)=>{return id==null?null:((IdWord)id).Value.ToByteArr();},
				(val)=>{return val==null?null:new IdWord(ToolId.ByteArrToUInt128((u8[])val));}
			);
			o.SetCol(nameof(PoWord.Owner)).HasConversion(
				(id)=>{return id==null?null:((IdUser)id).Value.ToByteArr();},
				(val)=>{return val==null?null:new IdUser(ToolId.ByteArrToUInt128((u8[])val));}
			);
		}

		var Tbl_Prop = Mk("Prop", PoKv.Example);
		Mgr.AddTable<PoKv>(Tbl_Prop);
		{
			var o = Tbl_Prop;
			CfgPoBase<PoKv>(o);
			o.CodeIdName = nameof(PoKv.Id);
			o.SetCol(nameof(PoKv.Id)).HasConversion(
				(id)=>{return id==null?null:((IdKv)id).Value.ToByteArr();},
				(val)=>{return val==null?null:new IdKv(ToolId.ByteArrToUInt128((u8[])val));}
			);
			o.SetCol(nameof(PoKv.FKeyUInt128)).HasConversion(
				(id)=>{return id==null?null:((UInt128)id).ToByteArr();},
				(val)=>{return val==null?null:ToolId.ByteArrToUInt128((u8[])val);}
			);
		}

		var Tbl_Learn = Mk("Learn", PoLearn.Example);
		Mgr.AddTable<PoLearn>(Tbl_Learn);
		{
			var o = Tbl_Learn;
			CfgPoBase<PoLearn>(o);
			o.CodeIdName = nameof(PoLearn.Id);
			o.SetCol(nameof(PoLearn.Id)).HasConversion(
				(id)=>{return id==null?null:((IdKv)id).Value.ToByteArr();},
				(val)=>{return val==null?null:new IdLearn(ToolId.ByteArrToUInt128((u8[])val));}
			);
			o.SetCol(nameof(PoLearn.FKeyUInt128)).HasConversion(
				(id)=>{return id==null?null:((UInt128)id).ToByteArr();},
				(val)=>{return val==null?null:ToolId.ByteArrToUInt128((u8[])val);}
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
