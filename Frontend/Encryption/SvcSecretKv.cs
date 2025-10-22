namespace Ngaq.Local.Domains.Encryption;

using Ngaq.Core.Shared.Encryption.Svc;
using Ngaq.Core.Shared.Kv.Models;
using Ngaq.Core.Sys.Models;
using Ngaq.Local.Db.TswG;
using Ngaq.Local.Domains.Kv.Dao;
using Ngaq.Local.Domains.Kv.Svc;
using Tsinswreng.CsSqlHelper;

public class SvcSecretKv : SvcKv, ISvcSecretKv {
	public SvcSecretKv(DaoKv DaoKv, TxnWrapper<DbFnCtx> TxnWrapper, IAppRepo<PoKv, IdKv> RepoKv) : base(DaoKv, TxnWrapper, RepoKv) {
	}
}
