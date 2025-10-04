using Ngaq.Core.Models.UserCtx;
using Ngaq.Core.Sys.Models;
using Ngaq.Core.Sys.Svc;
using Ngaq.Core.Word.Models.Po.Kv;
using Ngaq.Local.Db;
using Ngaq.Local.Db.TswG;
using Ngaq.Local.Sys.Dao;
using Tsinswreng.CsCore;
using Tsinswreng.CsPage;
using Tsinswreng.CsSqlHelper;

namespace Ngaq.Local.Sys.Svc;
using Z = SvcDbCfg;
public partial class SvcDbCfg(
	DaoCfg DaoCfg
	,TxnWrapper<DbFnCtx> TxnWrapper
	,IAppRepo<PoCfg, IdCfg> RepoCfg
)
	:ISvcDbCfg
{
	DaoCfg DaoCfg = DaoCfg;
	IAppRepo<PoCfg, IdCfg> RepoCfg = RepoCfg;
	//public const str PathSep = "/";


	[Impl(typeof(ISvcDbCfg))]
	public async Task<PoCfg?> GetOneByKStr(IUserCtx UserCtx, str Key, CT Ct){
		return await TxnWrapper.Wrap(FnGetOneByKStr, UserCtx, Key, Ct);
		//return await TxnWrapper.Wrap(new ClsGetOneByKStr(this), UserCtx, Key, Ct);
	}

	[Impl(typeof(ISvcDbCfg))]
	public async Task<nil> SetVStrByKStr(IUserCtx UserCtx, str Key, str Value, CT Ct){
		return await TxnWrapper.Wrap(FnAddOrSetVStrByKStr, UserCtx, Key, Value, Ct);
	}

	[Impl(typeof(ISvcDbCfg))]
	public async Task<nil> SetVI64ByKStr(IUserCtx UserCtx, str Key, i64 Value, CT Ct){
		return await TxnWrapper.Wrap(FnAddOrSetVI64ByKStr, UserCtx, Key, Value, Ct);
	}



	public async Task<Func<
		IUserCtx
		,str
		,CT
		,Task<PoCfg?>
	>> FnGetOneByKStr(IDbFnCtx Ctx, CT Ct){
		var PageQry = new PageQry{PageIdx = 0, PageSize = 1, WantTotCnt = false};
		var PageByKStr = await DaoCfg.FnPageByKStr(Ctx, Ct);
		var Fn = async(IUserCtx UserCtx, str Key, CT Ct)=>{
			var Page = await PageByKStr(UserCtx, Key, PageQry, Ct);
			if(Page.Data != null){
				var R = Page.Data.FirstOrDefault();
				return R;
			}
			return null;
		};
		return Fn;
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
		,CT
		,Task<nil>
	>> FnAddOrSetVStrByKStr(IDbFnCtx Ctx, CT Ct){
		var GetOneByKStr = await FnGetOneByKStr(Ctx, Ct);
		var Add = await FnAdd(Ctx, Ct);
		var SetVStrByKStr = await DaoCfg.FnSetVStrByKStr(Ctx, Ct);
		var Fn = async(IUserCtx UserCtx, str Key, str Value, CT Ct)=>{
			var Existing = await GetOneByKStr(UserCtx, Key, Ct);
			if(Existing != null){
				await SetVStrByKStr(UserCtx, Key, Value, Ct);
			}else{
				var ToAdd = new PoCfg{
					Owner = UserCtx.UserId,
					KStr = Key,
					VType = (i64)EKvType.Str,
					VStr = Value,
				};
				await Add(UserCtx, ToAdd, Ct);
			}
			return NIL;
		};
		return Fn;
	}

	public async Task<Func<
		IUserCtx
		,str
		,i64
		,CT
		,Task<nil>
	>> FnAddOrSetVI64ByKStr(IDbFnCtx Ctx, CT Ct){
		var GetOneByKStr = await FnGetOneByKStr(Ctx, Ct);
		var Add = await FnAdd(Ctx, Ct);
		var SetVI64ByKStr = await DaoCfg.FnSetVI64ByKStr(Ctx, Ct);
		var Fn = async(IUserCtx UserCtx, str Key, i64 Value, CT Ct)=>{
			var Existing = await GetOneByKStr(UserCtx, Key, Ct);
			if(Existing != null){
				await SetVI64ByKStr(UserCtx, Key, Value, Ct);
			}else{
				var ToAdd = new PoCfg{
					Owner = UserCtx.UserId,
					KStr = Key,
					VType = (i64)EKvType.I64,
					VI64 = Value,
				};
				await Add(UserCtx, ToAdd, Ct);
			}
			return NIL;
		};
		return Fn;
	}


#if false
	public class ClsGetOneByKStr(Z z):DbFn<IUserCtx, str, PoCfg?>{
		public async Task Init(IDbFnCtx Ctx, CT Ct){
			var PageQry = new PageQuery{PageIndex = 0, PageSize = 1, WantTotalCount = false};
			//var PageByKStr = await new DaoCfg.ClsPageByKStr.Init(Ctx, Ct);
			var PageByKStr = await new DaoCfg.ClsPageByKStr(z.DaoCfg).Init(Ctx, Ct);
			FnRun = async(IUserCtx UserCtx, str Key, CT Ct)=>{
				var Page = await PageByKStr.FnRun(UserCtx, Key, PageQry, Ct);
				if(Page.DataAsy != null){
					var R = await Page.DataAsy.FirstOrDefaultAsync(Ct);
					return R;
				}
				return null;
			};
		}
	}

	public class ClsAdd(Z z):DbFn<IUserCtx, PoCfg, nil>{
		public async Task<ClsAdd> Init(IDbFnCtx Ctx, CT Ct){
			var InsertMany = await z.RepoCfg.FnInsertMany(Ctx, Ct);
			FnRun = async(IUserCtx UserCtx, PoCfg PoCfg, CT Ct)=>{
				PoCfg.Owner = UserCtx.UserId;
				await InsertMany([PoCfg], Ct);
				return NIL;
			};
			return this;
		}
	}


#endif

}
