using Microsoft.Extensions.DependencyInjection;
using System;
using System.Data;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Model.UserCtx;
using Ngaq.Core.Service.Word;

//using Microsoft.CodeAnalysis.Text;
using Ngaq.Core.Tools;
using Ngaq.Core.Word;
using Ngaq.Core.Word.Svc;
using Ngaq.Local.Dao;
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


namespace Ngaq.Local;


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
z.AddSingleton<IDbConnection>(LocalDb.Inst.DbConnection);
//數據庫命令ˇ建者
z.AddScoped<ISqlCmdMkr, SqliteCmdMkr>();
//數據庫諸表ˇ司者
z.AddSingleton<ITblMgr>(LocalTblMgr.Inst);
//事務ˇ建者
z.AddScoped<I_GetTxnAsy, SqliteCmdMkr>();
//事務ˇ珩者
z.AddScoped<ITxnRunner, AdoTxnRunner>();
//數據庫初始化器
z.AddTransient<DbIniter>();
return z;
	}

//倉儲
	static IServiceCollection SetUpRepos(this IServiceCollection z){
z.AddScoped<Repo<SchemaHistory, i64>>();
z.AddScoped<Repo<PoWord, IdWord>>();
z.AddScoped<Repo<PoWordProp, IdWordProp>>();
z.AddScoped<Repo<PoWordLearn, IdLearn>>();
//z.AddScoped<IRunInTxn, AdoTxnRunner>();
return z;
	}

//服務類
	static IServiceCollection SetUpSvcs(this IServiceCollection z){
z.AddScoped<DaoSqlWord, DaoSqlWord>();
z.AddTransient<MgrLearn, MgrLearn>();
z.AddScoped<ISvcParseWordList, SvcParseWordList>();
z.AddScoped<ISvcWord, SvcWord>();
z.AddScoped<IImgGetter, SvcImg>();
z.AddScoped<IWeightCalctr, SvcWeight>();
return z;
	}

}
