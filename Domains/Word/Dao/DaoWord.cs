namespace Ngaq.Local.Word.Dao;

using Ngaq.Core.Model.Po;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Model.Po.Word;
using Tsinswreng.CsSqlHelper;
using Str_Any = System.Collections.Generic.Dictionary<str, obj?>;
using IStr_Any = System.Collections.Generic.IDictionary<str, obj?>;
using Tsinswreng.CsPage;
using Tsinswreng.CsTools;
using Ngaq.Local.Db.TswG;
using Ngaq.Core.Domains.Word.Models.Po.Kv;
using Ngaq.Core.Domains.Word.Models.Po.Word;
using Ngaq.Core.Domains.Word.Models.Po.Learn;

public partial class DaoSqlWord(
	ISqlCmdMkr SqlCmdMkr
	,ITblMgr TblMgr
	,IAppRepo<PoWord, IdWord> RepoWord
	,IAppRepo<PoWordProp, IdWordProp> RepoKv
	,IAppRepo<PoWordLearn, IdWordLearn> RepoLearn
){

	/// <summary>
	/// <delete>
	/// /// 予Svc層調用、不在Dao層調用。
	/// Dao層之操作更低級、直ᵈ操作數據庫、寡聚合、不作額外校驗
	/// 如FnInsertPoKvs中、內ʹ函數ʹ參數ˋ只受IEnumerable<PoWordProp>、未必潙同一詞ᐪ。且初加旹亦蜮調此 洏初加旹不璫更新UpdatedAt
	/// </delete>
	/// </summary>
	/// <param name="Ctx"></param>
	/// <param name="Ct"></param>
	/// <returns></returns>
	public async Task<Func<
		IdWord
		,CT
		,Task<nil>
	>> FnTriggerOnRootAfterUpd(IDbFnCtx? Ctx, CT Ct){
		var UpdPoWord = await RepoWord.FnUpd_BizUpdatedAt(Ctx,Ct);
		return async(WordId, Ct)=>{
			await UpdPoWord(WordId, Ct);
			return NIL;
		};
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
		).ToListTryNoCopy();
	}



	public obj? IdUpperToRaw<TPo>(obj UpperId){
		var T = TblMgr.GetTbl<TPo>();
		//不指定列名旹 UpperId潙obj、typeof<T> 得object 于默認轉換緩存詞典中尋不見
		return T.UpperToRaw(UpperId, nameof(I_Id<nil>.Id));
	}

	public async Task<Func<

		Task<nil>
	>> Fn(IDbFnCtx Ctx, CT Ct){

		var Fn = async()=>{
			return NIL;
		};
		return Fn;
	}

#if false
	public async Task<Func<
		Task<nil>
	>> Fn(){
		var Fn = async()=>{
			return NIL;
		};
		return Fn;
	}
#endif
}


