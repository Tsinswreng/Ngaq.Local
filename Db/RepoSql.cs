namespace Ngaq.Local.Db;

using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Ngaq.Core.Infra.Core;
using Ngaq.Core.Infra.Db;
using Ngaq.Core.Model.Po;
using Microsoft.Data.Sqlite;
using Ngaq.Core.Infra;
using Tsinswreng.CsSqlHelper;
using Ngaq.Core.Model;
using System.Collections;
using Tsinswreng.CsSqlHelper.Cmd;
using Tsinswreng.CsCore.Tools;
using Tsinswreng.CsSrcGen.DictMapper;

public class AppRepo<
	TEntity
	, TId
>
	: Tsinswreng.CsSqlHelper.Repo<TEntity, TId>
	where TEntity : class, new()
// where TEntity: class, I_Id<TId>, new()
// where TId : IEquatable<TId>

{
	public AppRepo(ITblMgr TblMgr, ISqlCmdMkr SqlCmdMkr, IDictMapper DictMapper) : base(TblMgr, SqlCmdMkr, DictMapper) {
	}
}
