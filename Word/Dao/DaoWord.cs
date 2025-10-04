namespace Ngaq.Local.Word.Dao;

using Ngaq.Core.Infra;
using Ngaq.Core.Model.Po;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Local.Db;
using Tsinswreng.CsSqlHelper;
using Str_Any = System.Collections.Generic.Dictionary<str, obj?>;
using IStr_Any = System.Collections.Generic.IDictionary<str, obj?>;
using Ngaq.Core.Infra.Errors;
using Ngaq.Core.Models.Po;
using Ngaq.Core.Word.Models.Po.Learn;
using Tsinswreng.CsPage;
using Ngaq.Core.Word.Models;
using Ngaq.Core.Models.UserCtx;
using Tsinswreng.CsTools;
using Ngaq.Local.Db.TswG;
using Ngaq.Core.Model.Sys.Po.User;
using System.Diagnostics;
using Ngaq.Core.Word.Models.Dto;
using Ngaq.Core.Model.UserCtx;
using Ngaq.Core.Word.Models.Po.Word;
using Ngaq.Core.Word.Models.Po.Kv;

public partial class DaoSqlWord(
	ISqlCmdMkr SqlCmdMkr
	,ITblMgr TblMgr
	,IAppRepo<PoWord, IdWord> RepoWord
	,IAppRepo<PoWordProp, IdWordProp> RepoKv
	,IAppRepo<PoWordLearn, IdWordLearn> RepoLearn
){

	public async Task<Func<
		IUserCtx
		,IdWord
		,CT
		,Task<nil>
	>> FnTriggerOnRootAfterUpd(IDbFnCtx Ctx, CT Ct){
		var UpdPoWord = await RepoWord.FnUpd_UpdatedAt(Ctx,Ct);
		return async(User, WordId, Ct)=>{
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


// 	public async Task<Func<
// 		IUserCtx
// 		,obj //IdWordProp or IdWordLearn
// 		,IdWord//New
// 		,CT
// 		,Task<nil>
// 	>> FnUpdAssetsForeignWordIdById(IDbFnCtx Ctx, CT Ct){
// 		// var TP = TblMgr.GetTbl<PoWordProp>();
// 		// var TL = TblMgr.GetTbl<PoWordLearn>();
// 		var UpdRawPropIdById = await RepoKv.FnUpdOneColById(Ctx, nameof(PoWordProp.WordId), Ct);
// 		var UpdRawLearnIdById = await RepoLearn.FnUpdOneColById(Ctx, nameof(PoWordLearn.WordId), Ct);
// 		// var NWordId = nameof(I_WordId.WordId); var NId = nameof(I_Id<nil>.Id);
// 		// var PWordId = TP.Prm(NWordId);
// // 		var GenSql = (ITable T)=>{
// // return
// // $"""
// // UPDATE {T.DbTblName}
// // SET {T.Fld(NWordId)} = {PWordId}
// // WHERE 1=1
// // AND {T.Fld(nameof(IPoBase.DelId))} IS NULL
// // AND {T.Fld(NWordId)} = {T.Prm(NId)}
// // """;
// // 		};
// // 		var SqlCmdP = SqlCmdMkr.Prepare(Ctx, GenSql(TP), Ct);
// // 		var SqlCmdW = SqlCmdMkr.Prepare(Ctx, GenSql(TL), Ct);
// 		return async(User, IdObj, NeoWordId, Ct)=>{

// 		};
// 	}
	public obj? IdUpperToRaw<TPo>(obj UpperId){
		var T = TblMgr.GetTbl<TPo>();
		//不指定列名旹 UpperId潙obj、typeof<T> 得object 于默認轉換緩存詞典中尋不見
		return T.UpperToRaw(UpperId, nameof(I_Id<nil>.Id));
	}

	// public async Task<Func<
	// 	IUserCtx
	// 	,IdWord
	// 	,CT
	// 	,Task<nil>
	// >> FnSoftDelJnWordById(IDbFnCtx Ctx, CT Ct){
	// 	var TW = TblMgr.GetTbl<PoWord>();
	// 	var NId = nameof(I_Id<nil>.Id);
	// 	var NWordId = nameof(I_WordId.WordId);
	// 	var WordSoftDelManyByKeys = await RepoWord.FnSoftDelManyByKeys(Ctx, NId, 99, Ct);
	// 	var PropSofDelManyByKeys = await RepoKv.FnSoftDelManyByKeys(Ctx, NWordId, 99, Ct);
	// 	var LearnSofDelManyByKeys = await RepoLearn.FnSoftDelManyByKeys(Ctx, NWordId, 99, Ct);
	// 	return async(User, IdWord, Ct)=>{
	// 		var RawIdWord = TW.UpperToRaw(IdWord);
	// 		await WordSoftDelManyByKeys([RawIdWord], Ct);
	// 		await PropSofDelManyByKeys([RawIdWord], Ct);
	// 		await LearnSofDelManyByKeys([RawIdWord], Ct);
	// 		return NIL;
	// 	};
	// }

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


