using Tsinswreng.CsSqlHelper.Sqlite;

namespace Ngaq.Local.Db.TswG;

public class LocalTblMgr:SqliteTblMgr{
protected static LocalTblMgr? _Inst = null;
public static LocalTblMgr Inst => _Inst??= new LocalTblMgr();

}
