

namespace Ngaq.Local.TsNgaq;

using Ngaq.Local.Db;
using Ngaq.Local.Db.TswG;
using Tsinswreng.CsPage;
using Tsinswreng.CsSqlHelper;
using E = Ngaq.Local.TsNgaq.TsNgaqEntities;

public class TsNgaqSvc(
	ISqlCmdMkr SqlCmdMkr
	,ITblMgr TblMgr
	,IAppRepo<E.textWord, i64> RepoTextWord
	,IAppRepo<E.property, i64> RepoProp
	,IAppRepo<E.learn, i64> RepoLearn
){

	public async Task<Func<
		CT, Task<IList<E.TsJnWord>>
	>> FnGetAllTsJnWord(IDbFnCtx Ctx, CT Ct){
		var PageTextWord = await RepoTextWord.FnPageAll(Ctx, Ct);
		var PageProp = await RepoProp.FnPageAll(Ctx, Ct);
		var PageLearn = await RepoLearn.FnPageAll(Ctx, Ct);
		var Fn = async(CT Ct)=>{
			var SlctAll = PageQry.SlctAll();
			var textWordPage = await PageTextWord(SlctAll, Ct);
			var propPage = await PageProp(SlctAll, Ct);
			var learnPage = await PageLearn(SlctAll, Ct);

			if(textWordPage.DataAsy == null || propPage.DataAsy == null || learnPage.DataAsy == null){
				throw new Exception("DataAsy is null");
			}
			var TextWords = await textWordPage.DataAsy.ToListAsync(Ct);
			var Props = await propPage.DataAsy.ToListAsync(Ct);
			var Learns = await learnPage.DataAsy.ToListAsync(Ct);

			var Id_TextWord = TextWords.Select(x=>x).ToDictionary(x=>x.id, x=>x);

			var Id_JnWord = TextWords.ToDictionary(x=>x.id, x=>new E.TsJnWord(x, [], []));

			foreach(var prop in Props){
				var WordId = prop.wid;
				if(Id_TextWord.TryGetValue(WordId, out var Word)){
					Id_JnWord[WordId].propertys.Add(prop);
				}
			}

			foreach(var learn in Learns){
				var WordId = learn.wid;
				if(Id_TextWord.TryGetValue(WordId, out var Word)){
					Id_JnWord[WordId].learns.Add(learn);
				}
			}
			//return Id_JnWord.Values.ToList();
			IList<E.TsJnWord> R = Id_JnWord.Values.ToList();
			return R;
		};

		return Fn;
	}

}
