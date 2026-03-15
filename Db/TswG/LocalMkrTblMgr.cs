namespace Ngaq.Local.Db.TswG;

using Tsinswreng.CsSql;

/// 爲遷移提供空白 TblMgr 實例（每次創建新實例，不共享全局狀態）
public class LocalMkrTblMgr: IMkrTblMgr{
	public ITblMgr MkTblMgr() => new LocalTblMgr();
}
