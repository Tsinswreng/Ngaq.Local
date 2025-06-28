using System.Data;
using Microsoft.Data.Sqlite;
using Ngaq.Core.Infra.Cfg;
using Tsinswreng.CsCfg;
using Tsinswreng.CsTools.Files;
using Tsinswreng.CsSqlHelper;

namespace Ngaq.Local.Db.TswG;

public class LocalDb{
protected static LocalDb? _Inst = null;
public static LocalDb Inst => _Inst??= new LocalDb();

	public ICfgAccessor CfgAccessor{ get; set; } = LocalCfg.Inst;

	public str DbPath{get;}
	//蜮 按需開關連接㕥代單例 更佳
	public IDbConnection DbConnection{get;set;}
	public LocalDb(){
		DbPath??=LocalCfgItems.Inst.SqlitePath.GetFrom(CfgAccessor)??throw new Exception();
		FileTool.EnsureFile(DbPath);
		DbConnection = new SqliteConnection($"Data Source={DbPath}");
		DbConnection.Open();
	}

	static LocalDb(){
		Inst.Init();
	}
	protected bool _Inited{get;set;} = false;

	public LocalDb Init(){
		if(_Inited){
			return this;
		}
		ITblMgr Mgr = LocalTblMgr.Inst;//TODO 自蔿己ʹ項目維護一TblMgr
		Mgr.DbSrcType = "Sqlite";
		new LocalTblMgrIniter(Mgr).Init();
		return this;
	}
}
