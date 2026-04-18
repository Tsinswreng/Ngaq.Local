namespace Ngaq.Backend.Frontend.Encryption;

using Ngaq.Core.Shared.Encryption.Svc;
using Ngaq.Core.Shared.Kv.Models;
using Ngaq.Backend.Db.TswG;
using Ngaq.Backend.Domains.Kv.Dao;
using Ngaq.Backend.Domains.Kv.Svc;
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
