namespace Ngaq.Local.Word.Dao;

using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Tsinswreng.CsSql;
using Str_Any = System.Collections.Generic.Dictionary<str, obj?>;
using IStr_Any = System.Collections.Generic.IDictionary<str, obj?>;
using Tsinswreng.CsPage;
using Tsinswreng.CsTools;
using Ngaq.Local.Db.TswG;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Shared.Word.Models.Po.Word;
using Ngaq.Core.Shared.Word.Models.Po.Learn;
using Ngaq.Core.Shared.Base.Models.Po;
using Tsinswreng.CsCore;

public partial class DaoWord(
	ISqlCmdMkr SqlCmdMkr
	,ITblMgr TblMgr
	, IRepo<PoWord, IdWord> RepoWord
	, IRepo<PoWordProp, IdWordProp> RepoProp
	, IRepo<PoWordLearn, IdWordLearn> RepoLearn
){

	protected ITable<PoWord> T => TblMgr.GetTbl<PoWord>();
	protected ITable<PoWord> TW => TblMgr.GetTbl<PoWord>();
	protected ITable<PoWordProp> TP => TblMgr.GetTbl<PoWordProp>();
	protected ITable<PoWordLearn> TL => TblMgr.GetTbl<PoWordLearn>();

	public Task<nil> BatAltWordAfterUpd(
		IDbFnCtx Ctx, IAsyncEnumerable<IdWord> Ids, CT Ct
	){
		return RepoWord.AsAppRepo().BatBizTouch(Ctx, Ids, Ct);
	}


	async Task<IList<TPo>> _PageToList<TPo>(
		IPage<IStr_Any> Page
		,ITable Tbl
	)where TPo:new()
	{
		if(Page.Data == null){
			return new List<TPo>();
		}
		return Page.Data.Select(
			D=>Tbl.AssignEntity(D, new TPo())
		).AsOrToList();
	}

	public obj? IdUpperToRaw<TPo>(obj UpperId){
		var T = TblMgr.GetTbl<TPo>();
		//不指定列名旹 UpperId潙obj、typeof<T> 得object 于默認轉換緩存詞典中尋不見
		return T.UpperToRaw(UpperId, nameof(I_Id<nil>.Id));
	}

}


