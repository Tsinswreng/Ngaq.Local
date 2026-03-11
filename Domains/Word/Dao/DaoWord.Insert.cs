namespace Ngaq.Local.Word.Dao;

using Tsinswreng.CsTools;
using Ngaq.Local.Db.TswG;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Shared.Word.Models;
using Ngaq.Core.Shared.Word.Models.Po.Word;
using Ngaq.Core.Shared.Word.Models.Po.Learn;
using Tsinswreng.CsSqlHelper;

public partial class DaoWord{

	public async Task<nil> BatInsertJnWord(
		IDbFnCtx Ctx
		,IAsyncEnumerable<IJnWord> Words
		,CT Ct
	){
		var W = Words;
		await RepoWord.BatInsert(Ctx, W.Select(x=>x.Word), Ct);
		await RepoProp.BatInsert(Ctx, W.Select(x=>x.Props).Flat(), Ct);
		await RepoLearn.BatInsert(Ctx, W.Select(x=>x.Learns).Flat(), Ct);
		return NIL;
	}
	
	
	public async Task<nil> BatInsertIdToProps(
		IDbFnCtx Ctx
		,IAsyncEnumerable<IdWord?> WordId
		,IAsyncEnumerable<IAsyncEnumerable<PoWordProp>> Props
		,CT Ct
	){
		var nonNullWordIds = WordId.Where(x=>x is not null).Select(x=>x.Value);
		await BatAltWordAfterUpd(Ctx, nonNullWordIds, Ct);
		await RepoProp.BatInsert(Ctx, Props.Flat(), Ct);
		return NIL;
	}

	public async Task<nil> BatInsertIdToLearns(
		IDbFnCtx Ctx
		,IAsyncEnumerable<IdWord?> WordId
		,IAsyncEnumerable<IAsyncEnumerable<PoWordLearn>> Learns
		,CT Ct
	){
		var nonNullWordIds = WordId.Where(x=>x is not null).Select(x=>x.Value);
		await BatAltWordAfterUpd(Ctx, nonNullWordIds, Ct);
		await RepoLearn.BatInsert(Ctx, Learns.Flat(), Ct);
		return NIL;
	}

	
}
