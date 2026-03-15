using Ngaq.Local.Db.TswG.Migrations;
using Tsinswreng.CsSql;

namespace Ngaq.Local.Db.TswG;

/// Local 端遷移清單註冊（僅保留業務選擇，不包含通用去重邏輯）。
public static class LocalMigrations{
	extension(IMigrationMgr z){
		/// 註冊 Local 端全部遷移。
		/// 通用去重策略由 CsSql.ExtnMigrationMgr 提供。
		public IMigrationMgr UseLocalMigrations(IServiceProvider Sp){
			z.AddMigrationsIfAbsent([
				new MInit(Sp)
				,new M2025_1224_134729(Sp)
			]);
			return z;
		}
	}
}
