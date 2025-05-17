// namespace Ngaq.Db;
// using Tsinswreng.SqlHelper;
// using Ngaq.Core.Model.Po.Kv;
// using Ngaq.Core.Model.Po.Learn;
// using Ngaq.Core.Model.Po.Word;
// using Ngaq.Core.Infra;




// public class TestTblInfo{
// 	static TestTblInfo(){
// 		Init();
// 	}
// 	public static void Init(){
// 		I_TableMgr Mgr = TableMgr.Inst;
// 		Mgr.AddTable<Po_Word>(Table.Mk("Word", Po_Word.Example));
// 		Mgr.AddTable<Po_Kv>(Table.Mk("Prop", Po_Kv.Example));
// 		Mgr.AddTable<Po_Learn>(Table.Mk("Learn", Po_Learn.Example));
// 	}

// 	// public static str GenSql<T_Po>()where T_Po:new(){
// 	// 	var Table = TableMgr.Inst.GetTable<T_Po>();
// 	// 	var name = Table.Name;
// 	// 	var sql = $"SELECT * FROM {name}";
// 	// 	var po = new T_Po();
// 	// 	var dict = DictCtx.ToDict(po);

// 	// 	foreach(var kv in dict){
// 	// 		System.Console.WriteLine(kv.Key);
// 	// 		System.Console.WriteLine(kv.Value);
// 	// 	}
// 	// 	return sql;
// 	// }
// }
