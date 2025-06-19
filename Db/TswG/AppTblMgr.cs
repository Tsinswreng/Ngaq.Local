using Tsinswreng.CsSqlHelper.Sqlite;

namespace Ngaq.Local.Db.TswG;

public class AppTblMgr:SqliteTblMgr{
protected static AppTblMgr? _Inst = null;
public static AppTblMgr Inst => _Inst??= new AppTblMgr();

}
