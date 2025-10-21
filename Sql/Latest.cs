namespace Ngaq.Local.Sql;

using Tsinswreng.CsTools;
using Tsinswreng.CsSqlHelper;
using Ngaq.Local.Db.TswG;
using Ngaq.Core.Domains.User.Models.Po.User;
using Ngaq.Core.Domains.User.Models.Po.Device;
using Ngaq.Core.Domains.User.Models;
using Ngaq.Core.Domains.Kv.Models;
using Tsinswreng.CsCfg;
using Ngaq.Core.Domains.Word.Models.Po.Kv;
using Ngaq.Local.Domains.Kv.Svc;
using Ngaq.Core.Domains.User.Svc;



/// <summary>
/// note: 建庫前勿用預編譯sql
/// TODO 使通用化
/// </summary>
public partial class DbIniter{

	public ISqlCmdMkr SqlCmdMkr;
	public ITxnRunner TxnRunner;
	public I_GetTxnAsy TxnGetter;
	public ITblMgr TblMgr;
	public IAppRepo<SchemaHistory, i64> RepoSchemaHistory;
	public ISvcKv SvcKv;
	public DbIniter(
		ISqlCmdMkr SqlCmdMkr
		,ITxnRunner TxnRunner
		,I_GetTxnAsy TxnGetter
		,ITblMgr TblMgr
		,IAppRepo<SchemaHistory, i64> RepoSchemaHistory
		,ISvcKv SvcKv
	){
		if(RepoSchemaHistory is not SqlRepo<SchemaHistory, i64> SqlRepoSchemaHistory){
			throw new ArgumentException("RepoSchemaHistory must be SqlRepo<SchemaHistory, i64>");
		}
		this.RepoSchemaHistory = RepoSchemaHistory;
		SqlRepoSchemaHistory.DictMapper = SqlHelperDictMapper.Inst;
		this.SvcKv = SvcKv;
		this.TblMgr = TblMgr;
		this.TxnRunner = TxnRunner;
		this.TxnGetter = TxnGetter;
		this.SqlCmdMkr = SqlCmdMkr;
		this.Sql = TblMgr.SqlMkSchema();

	}

	public long CreatedAt{get;set;} = 1749888405026;


	public async Task<Func<
		CT
		,Task<IAsyncEnumerable<IDictionary<str, object?>>>
	>> FnSelectSqliteMaster(IDbFnCtx DbFnCtx, CT Ct){
		var Sql = "SELECT * FROM sqlite_master";
		var Cmd = await SqlCmdMkr.MkCmd(DbFnCtx, Sql, Ct);
		var Fn = async(CT Ct)=>{
			return Cmd.IterIAsy(Ct);
		};
		return Fn;
	}

	public async Task<Func<
		CT
		,Task<nil>
	>> FnMkSchema(IDbFnCtx DbFnCtx, CT Ct){
		var Cmd = await SqlCmdMkr.MkCmd(DbFnCtx, Sql, Ct);//勿 Prepare、表未建好旹預無法編譯
		var Fn = async(CT Ct)=>{
			try{
				await Cmd.IterIAsy(Ct).FirstOrDefaultAsync(Ct);
				return NIL;
			}
			catch (System.Exception e){
				throw new Exception(
					"MkSchema failed\nSql:\n"
					+Sql+"\n"
					,e
				);

			}
		};
		return Fn;
	}

	public async Task<Func<
		CT
		,Task<nil>
	>> FnInit(IDbFnCtx DbFnCtx, CT Ct){
		var MkSchema = await FnMkSchema(DbFnCtx, Ct);
		var InsertSchemaHistory = await RepoSchemaHistory.FnInsertManyNoPrepare(DbFnCtx, Ct);
		var SelectSqliteMaster = await FnSelectSqliteMaster(DbFnCtx, Ct);
		var Fn = async(CT Ct)=>{
			var Items = await SelectSqliteMaster(Ct);
			var First = await Items.FirstOrDefaultAsync(Ct);
			if(First != null){
				return NIL;
			}
			var SchemaHistory = new SchemaHistory{
				CreatedMs = this.CreatedAt
				,Name = "Init"
			};
			await MkSchema(Ct);
			await InsertSchemaHistory([SchemaHistory], Ct);
			return NIL;
		};
		return Fn;
	}

	public async Task<nil> Init(CT Ct) {
		var Ctx = new DbFnCtx{Txn = await TxnGetter.GetTxnAsy(Ct)};
		var Init = await FnInit(Ctx, Ct);
		await TxnRunner.RunTxn(Ctx.Txn, Init, Ct);
		await InitKv(Ct);
		return NIL;
	}

	public async Task<nil> InitKv(CT Ct){
		IdUser? Owner = null;
		IdClient ClientId = new();
		var PoKv = new PoKv();
		PoKv.Owner = Owner;
		var Path = KeysClientKv.ClientId;
		PoKv.SetStr(Path, ClientId+"");
		await SvcKv.AddOrUpd(PoKv, Ct);
		return NIL;
	}
}
