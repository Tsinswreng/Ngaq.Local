using System.Data;
using Microsoft.Data.Sqlite;
using Ngaq.Core.Infra.Cfg;
using Tsinswreng.CsCore.Files;
using Tsinswreng.CsSqlHelper;

namespace Ngaq.Local.Db.TswG;

public class AppDb{
protected static AppDb? _Inst = null;
public static AppDb Inst => _Inst??= new AppDb();

	public str DbPath{get;} = AppCfgItems.Inst.SqlitePath.Get()??throw new Exception();
	public IDbConnection DbConnection{get;set;}
	public AppDb(){
		FileTool.EnsureFile(DbPath);
		DbConnection = new SqliteConnection($"Data Source={DbPath}");
		DbConnection.Open();
	}

	static AppDb(){
		Inst.Init();
	}
	protected bool _Inited{get;set;} = false;

	public AppDb Init(){
		if(_Inited){
			return this;
		}
		ITblMgr Mgr = AppTblMgr.Inst;//TODO 自蔿己ʹ項目維護一TblMgr
		Mgr.DbSrcType = "Sqlite";
		new TblMgrIniter(Mgr).Init();
		return this;
	}
}
