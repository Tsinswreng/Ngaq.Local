using Microsoft.Extensions.DependencyInjection;
using Ngaq.Core.Infra;
using Tsinswreng.CsSql;
using Tsinswreng.CsSql.Sqlite;

namespace Ngaq.Backend.Db.TswG.Migrations;

/// Local 端初始遷移。
///
/// 作用：
/// - 建立最初版本就存在的表
/// - 作爲後續所有增量遷移的起點
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
	/// Local 初始版本的 CreatedMs。
	public override i64 CreatedMs{get;set;} = 1749888405026;


	/// 只註冊「初始版本已有」的表，
	/// 不包含後面版本纔加入的新結構。
	void Init(){
		var Mgr = MkrTblMgr.MkTblMgr();
		// 遷移歷史表本身也屬於初始建庫內容的一部分。
		Mgr.AddTbl(new SchemaHistoryTblMkr().MkTbl());
		LocalTblMgrIniter.InitKv(Mgr);
		LocalTblMgrIniter.InitWord(Mgr);
		SqlsUp = Mgr.SqlsMkSchema();
	}


}

