namespace Ngaq.Local.Db;
using Tsinswreng.CsSqlHelper;
using Tsinswreng.CsDictMapper.DictMapper;

public class AppRepo<
	TEntity
	, TId
>
	: Tsinswreng.CsSqlHelper.Repo<TEntity, TId>
	where TEntity : class, new()
// where TEntity: class, I_Id<TId>, new()
// where TId : IEquatable<TId>

{
	public AppRepo(ITblMgr TblMgr, ISqlCmdMkr SqlCmdMkr, IDictMapperShallow DictMapper) : base(TblMgr, SqlCmdMkr, DictMapper) {
	}


}
