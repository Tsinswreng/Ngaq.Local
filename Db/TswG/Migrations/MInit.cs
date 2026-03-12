using Microsoft.Extensions.DependencyInjection;
using Ngaq.Core.Infra;
using Tsinswreng.CsSql;
using Tsinswreng.CsSql.Sqlite;

namespace Ngaq.Local.Db.TswG.Migrations;

public class MInit:SqlMigrationInfo{
	IServiceProvider Sp;
	IMkrTblMgr MkrTblMgr;
	public MInit(
		IServiceProvider Sp
	){
		this.Sp = Sp;
		this.MkrTblMgr = Sp.GetRequiredService<IMkrTblMgr>();
		Init();
	}
	public override i64 CreatedMs{get;set;} = 1749888405026;


	void Init(){
		var Mgr = MkrTblMgr.MkTblMgr();
		Mgr.AddTbl(new SchemaHistoryTblMkr().MkTbl());
		LocalTblMgrIniter.InitKv(Mgr);
		LocalTblMgrIniter.InitWord(Mgr);
		SqlsUp = Mgr.SqlsMkSchema();
	}


}

