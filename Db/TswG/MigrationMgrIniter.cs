using Ngaq.Backend.Db.TswG.Migrations;
using Tsinswreng.CsSql;

namespace Ngaq.Backend.Db.TswG;

/// Local 端遷移清單註冊（僅保留業務選擇，不包含通用去重邏輯）。
public static class LocalMigrations{
	extension(IMigrationMgr z){
		/// 註冊 Local 端全部遷移。
		/// 通用去重策略由 CsSql.ExtnMigrationMgr 提供。
		///
		/// 約定：
		/// - 這裏只列出「Local 端有哪些遷移」
		/// - 不在這裏做執行，不在這裏做歷史表寫入
		/// - 新增 Local 遷移時，只需要把新類追加到這個列表裏
		public IMigrationMgr UseLocalMigrations(IServiceProvider Sp){
			z.AddMigrationsIfAbsent([
				// 初始遷移：建立 Local 端最早期已有的表結構。
				new MInit(Sp)
				// 後續增量遷移：例如新增 StudyPlan 相關表。
				,new M2025_1224_134729(Sp)
			]);
			return z;
		}
	}
}
