using Microsoft.Extensions.DependencyInjection;
using Ngaq.Core.Infra;
using Tsinswreng.CsSql;

namespace Ngaq.Local.Db.TswG.Migrations;
using Self = M2025_1224_134729;
public class M2025_1224_134729:SqlMigrationInfo{
	public override i64 CreatedMs{get;set;} = Tempus.FromIso("2025-12-24T13:47:29.376+08:00");

	IServiceProvider Sp;
	IMkrTblMgr MkrTblMgr;
	public M2025_1224_134729(
		IServiceProvider Sp
	){
		this.Sp = Sp;
		this.MkrTblMgr = Sp.GetRequiredService<IMkrTblMgr>();
		Init();
	}
	void Init(){
		var Mgr = MkrTblMgr.MkTblMgr();
		LocalTblMgrIniter.InitStudyPlan(Mgr);
		SqlsUp = Mgr.SqlsMkSchema();
	}
}
