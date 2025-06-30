using Ngaq.Local.Db;
using Tsinswreng.CsTools.Files;
using Tsinswreng.CsSqlHelper;

namespace Ngaq.Local.Sql;

/// <summary>
/// note: 建庫前勿用預編譯sql
/// </summary>
public partial class DbIniter{

	public ISqlCmdMkr SqlCmdMkr{get;set;}
	public ITxnRunner TxnRunner{get;set;}
	public I_GetTxnAsy TxnGetter{get;set;}
	public ITblMgr TblMgr{get;set;}
	public Repo<SchemaHistory, i64> RepoSchemaHistory{get;set;}
	public DbIniter(
		ISqlCmdMkr SqlCmdMkr
		,ITxnRunner TxnRunner
		,I_GetTxnAsy TxnGetter
		,ITblMgr TblMgr
		,Repo<SchemaHistory, i64> RepoSchemaHistory
	){
		this.RepoSchemaHistory = RepoSchemaHistory;
		RepoSchemaHistory.DictMapper = SqlHelperDictMapper.Inst;
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
	>> FnSelectSqliteMaster(Db.IDbFnCtx DbFnCtx, CT Ct){
		var Sql = "SELECT * FROM sqlite_master";
		var Cmd = await SqlCmdMkr.MkCmd(DbFnCtx, Sql, Ct);
		var Fn = async(CT Ct)=>{
			return Cmd.Run(Ct);
		};
		return Fn;
	}

	public async Task<Func<
		CT
		,Task<nil>
	>> FnMkSchema(Db.IDbFnCtx DbFnCtx, CT Ct){
		var Cmd = await SqlCmdMkr.MkCmd(DbFnCtx, Sql, Ct);//勿Prepare
		var Fn = async(CT Ct)=>{
			await Cmd.Run(Ct).FirstOrDefaultAsync(Ct);
			return NIL;
		};
		return Fn;
	}

	public async Task<Func<
		CT
		,Task<nil>
	>> FnInit(Db.IDbFnCtx DbFnCtx, CT Ct){
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
				CreatedAt = this.CreatedAt
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
		return NIL;
	}
}
