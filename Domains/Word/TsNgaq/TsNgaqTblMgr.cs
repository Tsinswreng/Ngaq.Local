namespace Ngaq.Local.TsNgaq;
using Tsinswreng.CsSqlHelper.Sqlite;

public partial class TsNgaqTblMgr:SqliteTblMgr{
	protected static TsNgaqTblMgr? _Inst = null;
	public static TsNgaqTblMgr Inst => _Inst??= new TsNgaqTblMgr();
}


