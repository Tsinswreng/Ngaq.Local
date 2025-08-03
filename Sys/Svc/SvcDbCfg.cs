using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.UserCtx;
using Ngaq.Core.Models.UserCtx;
using Ngaq.Core.Sys.Models;
using Ngaq.Local.Db;
using Ngaq.Local.Db.TswG;
using Ngaq.Local.Sys.Dao;
using Tsinswreng.CsPage;
using Tsinswreng.CsSqlHelper;

namespace Ngaq.Local.Sys.Svc;
public partial class SvcDbCfg(
	DaoCfg DaoCfg
	,TxnWrapper<DbFnCtx> TxnWrapper
	,IAppRepo<PoCfg, IdCfg> RepoCfg
)
{
	public const str PathSep = "/";

	public async Task<Func<
		IUserCtx
		,str
		,CT
		,Task<PoCfg?>
	>> FnGetOneByKStr(IDbFnCtx Ctx, CT Ct){
		var PageQry = new PageQuery{PageIndex = 0, PageSize = 1, WantTotalCount = false};
		var PageByKStr = await DaoCfg.FnPageByKStr(Ctx, Ct);
		var Fn = async(IUserCtx UserCtx, str Key, CT Ct)=>{
			var Page = await PageByKStr(UserCtx, Key, PageQry, Ct);
			if(Page.DataAsy != null){
				var R = await Page.DataAsy.FirstOrDefaultAsync(Ct);
				return R;
			}
			return null;
		};
		return Fn;
	}

	public async Task<PoCfg?> GetOneByKStr(IUserCtx UserCtx, str Key, CT Ct){
		return await TxnWrapper.Wrap(FnGetOneByKStr, UserCtx, Key, Ct);
	}

	public async Task<Func<
		IUserCtx
		,PoCfg
		,CT
		,Task<nil>
	>> FnAdd(IDbFnCtx Ctx, CT Ct){
		var InsertMany = await RepoCfg.FnInsertMany(Ctx, Ct);
		var Fn = async(IUserCtx UserCtx, PoCfg PoCfg, CT Ct)=>{
			PoCfg.Owner = UserCtx.UserId;
			await InsertMany([PoCfg], Ct);
			return NIL;
		};
		return Fn;
	}

	public async Task<Func<
		IUserCtx
		,str
		,str
		,Task<nil>
	>> FnAddOrSetVStrByKStr(IDbFnCtx Ctx, CT Ct){
		var GetOneByKStr = await FnGetOneByKStr(Ctx, Ct);
		var Add = await FnAdd(Ctx, Ct);
		var SetVStrByKStr = await DaoCfg.FnSetVStrByKStr(Ctx, Ct);
		var Fn = async(IUserCtx UserCtx, str Key, str Value)=>{
			var Existing = await GetOneByKStr(UserCtx, Key, Ct);
			if(Existing != null){
				await SetVStrByKStr(UserCtx, Key, Value, Ct);
			}else{
				var ToAdd = new PoCfg{
					Owner = UserCtx.UserId,
					KStr = Key,
					KType = (i64)EKvType.I64,
					VStr = Value,
				};
				await Add(UserCtx, ToAdd, Ct);
			}
			return NIL;
		};
		return Fn;
	}

}
