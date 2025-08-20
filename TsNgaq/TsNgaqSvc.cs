using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Word.Models;
using Ngaq.Core.Word.Models.Po.Learn;

namespace Ngaq.Local.TsNgaq;
using E = Ngaq.Local.TsNgaq.TsNgaqEntities;

public class TsNgaqSvc{
	// public static JnWord EntityToJnWord(
	// 	E.JnWord Old
	// 	,ref JnWord R
	// ){
	// 	R??= new JnWord();
	// 	static nil ToPoWord(E.textWord Old, ref PoWord R){
	// 		R??= new PoWord();
	// 		R.Head = Old.text;
	// 		R.CreatedAt = Old.ct;
	// 		R.UpdatedAt = Old.mt;
	// 		return NIL;
	// 	}
	// 	static nil ToPoLearn(E.learn Old, IdWord WordId, ref PoWordLearn R){
	// 		R??= new PoWordLearn();
	// 		R.WordId = WordId;
	// 		R.CreatedAt = Old.ct;
	// 		R.UpdatedAt = Old.mt;
	// 		R.LearnResult = Old.belong;
	// 	}
	// 	var poWord = R.PoWord;
	// 	ToPoWord(Old.textWord, ref poWord);
	// 	R.PoWord = poWord;
	// }
}
