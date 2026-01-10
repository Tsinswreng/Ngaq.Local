using Microsoft.Extensions.DependencyInjection;
using Ngaq.Local.Db.TswG.Migrations;
using Tsinswreng.CsSqlHelper;

namespace Ngaq.Local.Db.TswG;

public class MigrationMgrIniter{
	public IServiceProvider Sp{get;set;}
	public IMigrationMgr MigrationMgr{get;set;}
	public ISqlCmdMkr SqlCmdMkr;
	public IMkrTxn MkrTxn;
	public MigrationMgrIniter(IServiceProvider Sp){
		this.Sp = Sp;
		this.MigrationMgr = Sp.GetRequiredService<MigrationMgr>();
		this.SqlCmdMkr = Sp.GetRequiredService<ISqlCmdMkr>();
		this.MkrTxn = Sp.GetRequiredService<IMkrTxn>();
		Init();
	}
	void Init(){
		MigrationMgr
		.AddMigration(ToMigration(new MInit(Sp)))
		.AddMigration(ToMigration(new M2025_1224_134729(Sp)))
		;
	}

	public IMigration ToMigration(ISqlMigrationInfo SqlMigrationInfo){
		return SqlMigration.MkSqlMigration(
			SqlCmdMkr: SqlCmdMkr
			,MkrTxn: MkrTxn
			,SqlMigrationInfo:SqlMigrationInfo
		);
	}
}
