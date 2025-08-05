using Microsoft.Extensions.DependencyInjection;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Model.UserCtx;
using Ngaq.Core.Service.Word;

//using Microsoft.CodeAnalysis.Text;
using Ngaq.Core.Word.Svc;
using Ngaq.Local.Service.Word;
using Ngaq.Local.Word.Svc;
using Tsinswreng.CsSqlHelper;
using Ngaq.Local.Db.TswG;
using Ngaq.Local.Sql;
using Ngaq.Core.Infra;
using Ngaq.Core.FrontendIF;
using Ngaq.Local.ImplFrontend;
using Tsinswreng.CsSqlHelper.Sqlite;
using Ngaq.Core.Word.Models.Po.Learn;
using Ngaq.Local.Infra;
using Tsinswreng.CsDictMapper;
using Ngaq.Local.Db;
using Ngaq.Core.Models.UserCtx;
using Ngaq.Local.Word.Dao;
using Ngaq.Core.Sys.Svc;
using Ngaq.Local.Sys.Svc;


namespace Ngaq.Local.Di;


public static class DiLocal{

	public static IServiceCollection SetUpLocal(this IServiceCollection z){
		z.SetUpDbCfg().SetUpRepos().SetUpSvcs();
//Core詞典映射
z.AddSingleton<IDictMapperShallow>(CoreDictMapper.Inst);
z.AddScoped<IUserCtxMgr, UserCtxMgr>();
//baseUrl
z.AddSingleton<I_GetBaseUrl, BaseUrl>();
return z;
	}



//數據庫配置
	static IServiceCollection SetUpDbCfg(this IServiceCollection z){
//數據庫連接單例
z.AddSingleton(LocalDb.Inst.DbConnection);
//數據庫命令ˇ建者
z.AddScoped<ISqlCmdMkr, SqliteCmdMkr>();
//數據庫諸表ˇ司者
z.AddSingleton<ITblMgr>(LocalTblMgr.Inst);
//事務ˇ建者
z.AddScoped<I_GetTxnAsy, SqliteCmdMkr>();
//事務ˇ珩者
z.AddScoped<ITxnRunner, AdoTxnRunner>();
z.AddScoped<IDbFnCtxMkr<DbFnCtx> ,DbFnCtxMkr<DbFnCtx>>();
z.AddScoped<TxnWrapper<DbFnCtx>>();
//數據庫初始化器
z.AddTransient<DbIniter>();
return z;
	}

//倉儲
	static IServiceCollection SetUpRepos(this IServiceCollection z){
z.AddScoped<
	IAppRepo<SchemaHistory, i64>
	,AppRepo<SchemaHistory, i64>
>();
z.AddScoped<
	IAppRepo<PoWord, IdWord>
	,AppRepo<PoWord, IdWord>
>();
z.AddScoped<
	IAppRepo<PoWordProp, IdWordProp>
	,AppRepo<PoWordProp, IdWordProp>
>();
z.AddScoped<
	IAppRepo<PoWordLearn, IdLearn>
	,AppRepo<PoWordLearn, IdLearn>
>();
//z.AddScoped<IRunInTxn, AdoTxnRunner>();
return z;
	}


//服務類
	static IServiceCollection SetUpSvcs(this IServiceCollection z){
z.AddScoped<DaoSqlWord, DaoSqlWord>();
z.AddScoped<ISvcParseWordList, SvcParseWordList>();
z.AddScoped<ISvcWord, SvcWord>();
z.AddScoped<ISvcDbCfg, SvcDbCfg>();
z.AddScoped<IImgGetter, SvcImg>();
z.AddScoped<TxnWrapper<DbFnCtx>>();
return z;
	}

}
