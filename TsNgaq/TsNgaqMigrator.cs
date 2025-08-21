using Ngaq.Core.Word.Models;
using Ngaq.Core.Word.Svc;
using Ngaq.Local.Db;
using Ngaq.Local.Word.Dao;

namespace Ngaq.Local.TsNgaq;
using E = Ngaq.Local.TsNgaq.TsNgaqEntities;
public class TsNgaqMigrator{
	public str TsNagqDbPath{get;set;}

	public TsNgaqMigrator(str TsNagqDbPath){
		this.TsNagqDbPath = TsNagqDbPath;
	}

	public async Task<IList<JnWord>> ToJnWords(CT Ct){
		var Dependency = new TsNgaqDependency(TsNagqDbPath);
		var Svc = Dependency.TsNgaqSvc;
		var Ctx = new DbFnCtx();
		var GetAllTsJnWord = await Svc.FnGetAllTsJnWord(Ctx, Ct);
		var TsJnWords = await GetAllTsJnWord(Ct);
		var JnWords = TsJnWords.Select(x=>E.ToNewJnWord(x)).ToList();
		return JnWords;
	}
}
