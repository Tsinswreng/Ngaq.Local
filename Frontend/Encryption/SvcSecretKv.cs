namespace Ngaq.Local.Frontend.Encryption;

using Ngaq.Core.Shared.Encryption.Svc;
using Ngaq.Core.Shared.Kv.Models;
using Ngaq.Core.Sys.Models;
using Ngaq.Local.Db.TswG;
using Ngaq.Local.Domains.Kv.Dao;
using Ngaq.Local.Domains.Kv.Svc;
using Tsinswreng.CsSql;

public class SvcSecretKv : SvcKv, ISvcSecretKv {
	public SvcSecretKv(
		DaoKv DaoKv
		,TxnWrapper TxnWrapper
		,IRepo<PoKv, IdKv> RepoKv
		,ISqlCmdMkr SqlCmdMkr
	)
	: base(DaoKv, TxnWrapper, RepoKv, SqlCmdMkr) {
	}
}
