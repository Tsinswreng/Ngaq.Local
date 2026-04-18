using Microsoft.Extensions.DependencyInjection;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;

//using Microsoft.CodeAnalysis.Text;
using Tsinswreng.CsSql;
using Ngaq.Local.Db.TswG;
using Ngaq.Local.Sql;
using Ngaq.Core.Infra;
using Ngaq.Local.ImplFrontend;
using Tsinswreng.CsSql.Sqlite;
using Ngaq.Local.Word.Dao;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Local.Domains.Kv.Svc;
using Ngaq.Local.Domains.Kv.Dao;
using Ngaq.Core.Shared.Kv.Models;
using Ngaq.Core.Shared.Word.Models.Po.Word;
using Ngaq.Core.Shared.Word.Models.Po.Learn;
using Ngaq.Core.Frontend.ImgBg;
using Ngaq.Core.Shared.Kv.Svc;
using Ngaq.Local.Domains.Word.Svc;
using Ngaq.Core.Shared.Word.Svc;
using Ngaq.Core.Infra.Url;
using Ngaq.Core.Shared.Dictionary.Svc;
using Ngaq.Local.Domains.Dictionary.Svc;
using Tsinswreng.Srefl;
using Ngaq.Core.Shared.StudyPlan.Models.Po.StudyPlan;
using Ngaq.Core.Shared.StudyPlan.Models.Po.WeightArg;
using Ngaq.Core.Shared.StudyPlan.Models.Po.WeightCalculator;
using Ngaq.Core.Shared.StudyPlan.Models.Po.PreFilter;
using Ngaq.Local.Domains.StudyPlan.Dao;
using Ngaq.Local.Domains.StudyPlan.Svc;
using Ngaq.Core.Shared.StudyPlan.Svc;
using Ngaq.Core.Shared.Word.Models.Po.UserLang;
using Ngaq.Core.Shared.Word.Models.Po.NormLangToUserLang;
using Ngaq.Core.Shared.Dictionary.Models.Po.NormLang;
using Ngaq.Core.Shared.Audio;
using Ngaq.Local.Domains.Word.Dao;
using Tsinswreng.CsCore;


namespace Ngaq.Local.Di;

[Doc("僅限本地(客戶端)後端使用、勿用于Server")]
public static class DiLocal{

	public static IServiceCollection SetupLocal(this IServiceCollection z){
		z.AddSingleton<I_GetBaseDir>(BaseDirMgr.Inst);
		z.SetupDbCfg().SetupRepos().SetupCommonBackend();
		//Core詞典映射
		z.AddSingleton<IPropAccessorReg>(CoreDictMapper.Inst.PropAccessorReg);
		return z;
	}



//數據庫配置
	static IServiceCollection SetupDbCfg(this IServiceCollection z){
//數據庫連接單例
z.AddSingleton(LocalDb.Inst.DbConnection);
z.AddSingleton<IDbConnMgr, SingletonDbConnGetter>();
//數據庫命令ˇ建者
z.AddScoped<ISqlCmdMkr, SqliteCmdMkr>();
//數據庫諸表ˇ司者
z.AddSingleton<ITblMgr>(LocalTblMgr.Inst);
//遷移用空白 TblMgr 工廠
z.AddSingleton<IMkrTblMgr, LocalMkrTblMgr>();
//事務ˇ建者
z.AddScoped<IMkrTxn, SqliteCmdMkr>();
//事務ˇ珩者
z.AddScoped<ITxnRunner, AdoTxnRunner>();
z.AddScoped<IMkrDbFnCtx ,MkrDbFnCtx>();
z.AddScoped<TxnWrapper>();
//數據庫初始化器
z.AddTransient<DbIniter>();
// 通用遷移執行邏輯在 CsSql；此處僅聲明 Local 端遷移清單
z.AddSingleton<IMigrationMgr>(sp=>
	new MigrationMgr(
		TblMgr: sp.GetRequiredService<ITblMgr>()
		,SqlCmdMkr: sp.GetRequiredService<ISqlCmdMkr>()
	)
	.UseLocalMigrations(sp)
);
return z;
	}

	static IServiceCollection AddRepoScoped<TEntity, TId>(
		this IServiceCollection z
	)where TEntity:class, new()
	{
		//z.AddScoped<IRepo<TEntity, TId>, EfRepo<TEntity, TId>>();
		z.AddScoped<IRepo<TEntity, TId>, AppRepo<TEntity, TId>>();
		return z;
	}

//倉儲
	static IServiceCollection SetupRepos(this IServiceCollection z){
z.AddRepoScoped<SchemaHistory, i64>();
z.AddRepoScoped<PoWord, IdWord>();
z.AddRepoScoped<PoWordProp, IdWordProp>();
z.AddRepoScoped<PoWordLearn, IdWordLearn>();
z.AddRepoScoped<PoUserLang, IdUserLang>();
z.AddRepoScoped<PoNormLang, IdNormLang>();
z.AddRepoScoped<PoNormLangToUserLang, IdNormLangToUserLang>();
z.AddRepoScoped<PoKv, IdKv>();
z.AddRepoScoped<PoStudyPlan, IdStudyPlan>();
z.AddRepoScoped<PoWeightArg, IdWeightArg>();
z.AddRepoScoped<PoWeightCalculator, IdWeightCalculator>();
z.AddRepoScoped<PoPreFilter, IdPreFilter>();
//z.AddScoped<IRunInTxn, AdoTxnRunner>();
return z;
	}


//服務類

	
	

}
