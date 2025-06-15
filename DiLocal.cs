using Microsoft.Extensions.DependencyInjection;
using System;
using System.Data;
using Ngaq.Core.Infra.Db;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Model.UserCtx;
using Ngaq.Core.Service.Word;

//using Microsoft.CodeAnalysis.Text;
using Ngaq.Core.Tools;
using Ngaq.Core.Word;
using Ngaq.Core.Word.Svc;
using Ngaq.Db;
using Ngaq.Local.Dao;
using Ngaq.Local.Db;
using Ngaq.Local.Service.Word;
using Ngaq.Local.Word.Svc;
using Tsinswreng.CsSqlHelper;
using Tsinswreng.CsSqlHelper.Cmd;
using Ngaq.Local.Db.TswG;
using Ngaq.Local.Sql;
using Tsinswreng.CsSrcGen.DictMapper;
using Ngaq.Core.Infra;


namespace Ngaq.Local;


public class DiLocal{

	public static IServiceCollection SetUpLocal(IServiceCollection z){


//z.AddDbContext<LocalDbCtx>();
//svc.AddScoped<DaoWord, DaoWord>();
z.AddSingleton<IDictMapper>(CoreDictMapper.Inst);
z.AddScoped<DaoSqlWord, DaoSqlWord>();
z.AddScoped<ISqlCmdMkr, SqliteCmdMkr>();
z.AddSingleton<IDbConnection>(AppTblInfo.Inst.DbConnection);
z.AddTransient<MgrLearn, MgrLearn>();
z.AddSingleton<ITblMgr>(AppTableMgr.Inst);

z.AddScoped<Repo<SchemaHistory, i64>>();
z.AddScoped<Repo<PoWord, IdWord>>();
z.AddScoped<Repo<PoWordProp, IdWordProp>>();
z.AddScoped<Repo<PoWordLearn, IdLearn>>();z.AddScoped<IRunInTxn, SqlTxnRunner>();

z.AddScoped<ITxnRunner, SqlTxnRunner>();
z.AddScoped<ISvcParseWordList, SvcParseWordList>();
z.AddScoped<ISvcWord, SvcWord>();
z.AddScoped<IWeightCalctr, SvcWeight>();
z.AddScoped<IUserCtxMgr, UserCtxMgr>();
z.AddScoped<IGetTxn, SqliteCmdMkr>();
z.AddTransient<DbIniter>();
return z;

	}

}
