namespace Ngaq.Local.Db.TswG;
using Tsinswreng.CsSqlHelper.Sqlite;

public partial class LocalTblMgr:SqliteTblMgr{
	public static LocalTblMgr Inst => field??= new LocalTblMgr();
}
