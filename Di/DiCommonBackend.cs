using Microsoft.Extensions.DependencyInjection;
using Ngaq.Core.Shared.Dictionary.Svc;
using Ngaq.Core.Shared.Kv.Svc;
using Ngaq.Core.Shared.StudyPlan.Svc;
using Ngaq.Core.Shared.Word.Svc;
using Ngaq.Backend.Domains.Dictionary.Svc;
using Ngaq.Backend.Domains.Kv.Dao;
using Ngaq.Backend.Domains.Kv.Svc;
using Ngaq.Backend.Domains.StudyPlan.Dao;
using Ngaq.Backend.Domains.StudyPlan.Svc;
using Ngaq.Backend.Domains.Word.Dao;
using Ngaq.Backend.Domains.Word.Svc;
using Ngaq.Backend.Word.Dao;
using Tsinswreng.CsCore;
using Microsoft.Extensions.Logging;

namespace Ngaq.Backend.Di;

public static class DiCommonBackend{
	
	[Doc(@$"註冊 本地後端與Web服務端 共同模塊 如SvcWord等")]
	public static IServiceCollection SetupCommonBackend(this IServiceCollection z){
		using var loggerFactory = LoggerFactory.Create(b=>{
			b.AddConsole()
			#if DEBUG
			.SetMinimumLevel(LogLevel.Debug)
			#else
			.SetMinimumLevel(LogLevel.Information)
			#endif
			;
		});
		var Logger = loggerFactory.CreateLogger("GlobalLogger");
		z.AddSingleton<ILogger>(Logger);
		SetupSvcs(z);
		return z;
	}
	static IServiceCollection SetupSvcs(this IServiceCollection z){
		SetupWord(z);
		SetupStudyPlan(z);
z.AddScoped<DaoKv, DaoKv>();
z.AddScoped<ISvcDictionary, SvcDictionary>();
z.AddScoped<ISvcKv, SvcKv>();

return z;
	}
	
	static IServiceCollection SetupWord(this IServiceCollection z){
		z.AddScoped<DaoWord, DaoWord>();
		z.AddScoped<DaoWordV2, DaoWordV2>();
		z.AddScoped<DaoUserLang, DaoUserLang>();
		z.AddScoped<DaoNormLang, DaoNormLang>();
		z.AddScoped<DaoNormLangToUserLang, DaoNormLangToUserLang>();
		z.AddScoped<ISvcParseWordList, SvcParseWordList>();
		z.AddScoped<ISvcWord, SvcWord>();
		z.AddScoped<ISvcWordV2, SvcWordV2>();
		z.AddScoped<ISvcWordInMem, SvcWordInMem>();
		z.AddScoped<ISvcUserLang, SvcUserLang>();
		z.AddScoped<ISvcNormLang, SvcNormLang>();
		z.AddScoped<ISvcNormLangToUserLang, SvcNormLangToUserLang>();
		return z;
	}
	static IServiceCollection SetupStudyPlan(this IServiceCollection z){
		z.AddScoped<DaoStudyPlan, DaoStudyPlan>();
		z.AddScoped<ISvcStudyPlan, SvcStudyPlan>();
		z.AddScoped<IStudyPlanGetter>(sp=>(IStudyPlanGetter)sp.GetRequiredService<ISvcStudyPlan>());
		z.AddScoped<PreFilterSqlMkr, PreFilterSqlMkr>();
		return z;
	}
	
	
}
