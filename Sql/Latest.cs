namespace Ngaq.Local.Sql;

using Tsinswreng.CsSql;
using Ngaq.Local.Db.TswG;
using Ngaq.Core.Shared.User.Models.Po.Device;
using Ngaq.Core.Shared.Kv.Models;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Frontend.Kv;
using Ngaq.Core.Shared.Kv.Svc;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Infra.IF;



/// <summary>
/// 客戶端程序啓動旹初始化數據庫
/// 未建庫則建庫、庫ʹ版本落後則珩遷移
/// note: 建庫前勿用預編譯sql
/// TODO 使通用化
/// </summary>
public partial class DbIniter{
	public ISqlCmdMkr SqlCmdMkr;
	public ITxnRunner TxnRunner;
	public IMkrTxn TxnGetter;
	public ITblMgr TblMgr;
	public Tsinswreng.CsSql.IRepo<SchemaHistory, i64> RepoSchemaHistory;
	public ISvcKv SvcKv;
	public IMigrationMgr MigrationMgr;
	public DbIniter(
		ISqlCmdMkr SqlCmdMkr
		,ITxnRunner TxnRunner
		,IMkrTxn TxnGetter
		,ITblMgr TblMgr
		, IRepo<SchemaHistory, i64> RepoSchemaHistory
		,ISvcKv SvcKv
		,IMigrationMgr MigrationMgr
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
		this.MigrationMgr = MigrationMgr;
		this.SqlMkSchema = TblMgr.SqlMkSchema();
	}

	/// <summary>
	/// 未建庫旹一步到位建庫ʃ用ʹSql
	/// </summary>
	public str SqlMkSchema{get;set;} ="";
	public long CreatedAt{get;set;} = 1749888405026;


	public async Task<Func<
		CT
		,Task<IAsyncEnumerable<IDictionary<str, object?>>>
	>> FnSelectSqliteMaster(IDbFnCtx DbFnCtx, CT Ct){
		var Sql = "SELECT * FROM sqlite_master";
		var Cmd = await SqlCmdMkr.MkCmd(DbFnCtx, Sql, Ct);
		var Fn = async(CT Ct)=>{
			return Cmd.AsyE1d(Ct);
		};
		return Fn;
	}

	/// <summary>
	/// 未建庫旹一步到位建庫ʃ用ʹSql
	/// </summary>
	/// <param name="DbFnCtx"></param>
	/// <param name="Ct"></param>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	public async Task<Func<
		CT
		,Task<nil>
	>> FnMkSchema(IDbFnCtx DbFnCtx, CT Ct){
		var Cmd = await SqlCmdMkr.MkCmd(DbFnCtx, SqlMkSchema, Ct);//勿 Prepare、表未建好旹預無法編譯
		var Fn = async(CT Ct)=>{
			try{
				await Cmd.AsyE1d(Ct).FirstOrDefaultAsync(Ct);
				return NIL;
			}
			catch (System.Exception e){
				throw new Exception(
					"MkSchema failed\nSql:\n"
					+SqlMkSchema+"\n"
					,e
				);

			}
		};
		return Fn;
	}

	void test(){
		var a = new List<str>();
		foreach(var (i,s) in a.Index()){

		}
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
		var Ctx = new DbFnCtx{};
		Ctx.Txn = await TxnGetter.MkEtBindTxn(Ctx, Ct);
		var Init = await FnInit(Ctx, Ct);
		await TxnRunner.RunTxn(Ctx.Txn, Init, Ct);
		await InitKv(Ct);
		return NIL;
	}

	public async Task<nil> InitKv(CT Ct){
		IdClient ClientId = new();
		var PoKv = new PoKv();
		var Path = KeysClientKv.ClientId;
		PoKv.SetStrStr(Path, ClientId+"");
		var Old = await SvcKv.GetByOwnerEtKey(IdUser.Zero, Path, Ct);
		if(Old is not null){
			return NIL;
		}
		await SvcKv.Set(PoKv, Ct);
		return NIL;
	}
}
