namespace Ngaq.Local.Db;

public class FreeSqlCfg {
	protected static FreeSqlCfg? _Inst = null;
	public static FreeSqlCfg Inst => _Inst??= new FreeSqlCfg();

	public IFreeSql Fsql{get;set;}
	FreeSqlCfg() {
		var dbPath = Path.Combine(
			Directory.GetCurrentDirectory(),
			"..", "Ngaq.sqlite"
		);
		var connectionString = $"Data Source={dbPath}";
		Fsql = new FreeSql.FreeSqlBuilder()
			.UseConnectionString(FreeSql.DataType.Sqlite, connectionString)
			//.UseAutoSyncStructure(true) //自动同步实体结构【开发环境必备】，FreeSql不会扫描程序集，只有CRUD时才会生成表。
			.UseMonitorCommand(cmd => Console.Write(cmd.CommandText))
			.Build(); //请务必定义成 Singleton 单例模式

		var Type_Ctx = typeof(DbCtx);
		System.Console.WriteLine(Type_Ctx);//->Ngaq.Local.Db.DbCtx
		System.Console.WriteLine(Type_Ctx == null);//False
		Fsql.CodeFirst.ApplyConfigurationFromEFCore(
			Type_Ctx
		);
	}
}
