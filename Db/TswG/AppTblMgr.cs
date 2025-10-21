namespace Ngaq.Local.Db.TswG;
using Tsinswreng.CsSqlHelper.Sqlite;

public partial class LocalTblMgr:SqliteTblMgr{
protected static LocalTblMgr? _Inst = null;
public static LocalTblMgr Inst => _Inst??= new LocalTblMgr();

}


