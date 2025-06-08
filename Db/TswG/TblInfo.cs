namespace Ngaq.Db;
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


	nil CfgPoBase<TPo>(ITable Tbl){
		var o = Tbl;
		o.SetCol(nameof(IPoBase.CreatedAt)).HasConversion<Tempus, i64>(
			tempus=>tempus.Value,
			val=>new Tempus(val)
		);
		o.SetCol(nameof(IPoBase.DbCreatedAt)).HasConversion<Tempus, i64>(
			tempus=>tempus.Value,
			val=>new Tempus(val)
		);
		o.SetCol(nameof(IPoBase.UpdatedAt)).HasConversion<Tempus?, i64?>(
			tempus=>tempus?.Value,
			val=>val==null?null:new Tempus(val.Value)
		);
		o.SetCol(nameof(IPoBase.UpdatedAt)).HasConversion<Tempus?, i64?>(
			tempus=>tempus?.Value,
			val=>val==null?null:new Tempus(val.Value)
		);

		o.SetCol(nameof(IPoBase.Status)).HasConversion<PoStatus, i64>(
			s=>s.Value,
			val=>new PoStatus(val)
		);

		o.SetCol(nameof(IPoBase.CreatedBy)).HasConversion<IdUser?, u8[]?>(
			(id)=>(id)?.Value.ToByteArr(),
			(val)=>val==null?null:IdUser.FromByteArr(val)
		);
		o.SetCol(nameof(IPoBase.LastUpdatedBy)).HasConversion<IdUser?, u8[]?>(
			(id)=>(id)?.Value.ToByteArr(),
			(val)=>val==null?null:IdUser.FromByteArr(val)
		);
		return NIL;
	}



	protected ITable Mk<T>(str Name, T Example){
		var ExDict = DictCtx.ToDictT(Example);
		return Table.Mk(
			DictCtx.DictMapper
			,Name
			,ExDict
		);
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
			o.SetCol(nameof(PoWord.Id)).HasConversion<IdWord, u8[]>(
				(id)=>(id).Value.ToByteArr(),
				(val)=>IdWord.FromByteArr(val)
			);
			o.SetCol(nameof(PoWord.Owner)).HasConversion<IdUser, u8[]>(
				(id)=>(id).Value.ToByteArr(),
				(val)=>IdUser.FromByteArr(val)
			);
		}

		var Tbl_Prop = Mk("Prop", PoKv.Example);
		Mgr.AddTable<PoKv>(Tbl_Prop);
		{
			var o = Tbl_Prop;
			CfgPoBase<PoKv>(o);
			o.CodeIdName = nameof(PoKv.Id);
			o.SetCol(nameof(PoKv.Id)).HasConversion<IdKv, u8[]>(
				(id)=>(id).Value.ToByteArr(),
				(val)=>IdKv.FromByteArr(val)
			);

			o.SetCol(nameof(PoKv.WordId)).HasConversion<IdWord, u8[]>(
				(id)=>(id).Value.ToByteArr(),
				(val)=>IdWord.FromByteArr(val)
			);

//TODO配置忽略之字段
			// o.SetCol(nameof(PoKv.FKeyUInt128)).HasConversion<UInt128, u8[]>(
			// 	(id)=>(id).ToByteArr(),
			// 	(val)=>ToolId.ByteArrToUInt128(val)
			// );

		}

		var Tbl_Learn = Mk("Learn", PoLearn.Example);
		Mgr.AddTable<PoLearn>(Tbl_Learn);
		{
			var o = Tbl_Learn;
			CfgPoBase<PoLearn>(o);
			o.CodeIdName = nameof(PoLearn.Id);
			o.SetCol(nameof(PoLearn.Id)).HasConversion<IdLearn, u8[]>(
				(id)=>(id).Value.ToByteArr(),
				(val)=>IdLearn.FromByteArr(val)
			);
			o.SetCol(nameof(PoLearn.WordId)).HasConversion<IdWord, u8[]>(
				(id)=>(id).Value.ToByteArr(),
				(val)=>IdWord.FromByteArr(val)
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
