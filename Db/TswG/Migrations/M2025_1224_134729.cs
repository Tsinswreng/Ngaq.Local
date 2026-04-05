using Microsoft.Extensions.DependencyInjection;
using Ngaq.Core.Infra;
using Tsinswreng.CsSql;

namespace Ngaq.Local.Db.TswG.Migrations;
using Self = M2025_1224_134729;

/// Local 端新增 StudyPlan 結構的增量遷移。
///
/// 它只關心「StudyPlan 這次新增了什麼」，
/// 不重新聲明初始版本的其它表。
public class M2025_1224_134729:SqlMigrationInfo{
	/// 本次 StudyPlan 遷移的版本號。
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
	/// 只註冊 StudyPlan 相關結構，再生成本次遷移所需 DDL。
	void Init(){
		var Mgr = MkrTblMgr.MkTblMgr();
		LocalTblMgrIniter.InitStudyPlan(Mgr);
		LocalTblMgrIniter.InitUserLang(Mgr);
		SqlsUp = Mgr.SqlsMkSchema();
	}
}
