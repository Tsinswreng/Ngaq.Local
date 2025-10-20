namespace Ngaq.Local.Domains.Kv.Svc;
using Ngaq.Core.Domains.User.Models;
using Ngaq.Core.Domains.User.Models.Po.User;
using Ngaq.Core.Domains.User.Svc;
using Ngaq.Core.Domains.User.UserCtx;
using Ngaq.Core.Domains.Word.Models.Po.Kv;
using Ngaq.Core.Sys.Models;
using Ngaq.Local.Db.TswG;
using Ngaq.Local.Domains.Kv.Dao;
using Tsinswreng.CsCore;
using Tsinswreng.CsPage;
using Tsinswreng.CsSqlHelper;

using Z = SvcKv;
public partial class SvcKv(
	DaoKv DaoKv
	,TxnWrapper<DbFnCtx> TxnWrapper
	,IAppRepo<PoKv, IdKv> RepoKv
)
	:ISvcKv
{
	DaoKv DaoKv = DaoKv;
	IAppRepo<PoKv, IdKv> RepoCfg = RepoKv;
	//public const str PathSep = "/";

	public Task<PoKv?> GetByOwnerEtKey(IdUser? Owner, obj Key, CT Ct){
		return TxnWrapper.Wrap(FnGetByOwnerEtKey, Owner, Key, Ct);
	}
	public Task<nil> AddOrUpdByOwnerEtKey(IdUser? Owner, obj Key, PoKv Po, CT Ct){
		return TxnWrapper.Wrap(FnAddOrUpdByOwnerEtKey, Owner, Key, Po, Ct);
	}


	public async Task<Func<
		IdUser?
		,obj
		,CT, Task<PoKv?>
	>> FnGetByOwnerEtKey(IDbFnCtx Ctx, CT Ct){
		var GetByKStr = await DaoKv.FnGetByOwnerEtKStr(Ctx, Ct);
		var GetByKI64 = await DaoKv.FnGetByOwnerEtKI64(Ctx, Ct);
		return async(IdUser? Owner, obj Key, CT Ct)=>{
			if(Key is i64 KI64){
				return await GetByKI64(Owner, KI64, Ct);
			}else{
				return await GetByKStr(Owner, Key+"", Ct);
			}
		};
	}

	public async Task<Func<
		IdUser?
		,obj
		,PoKv
		,CT, Task<nil>
	>> FnAddOrUpdByOwnerEtKey(IDbFnCtx Ctx, CT Ct){
		var GetByOwnerEtKey = await FnGetByOwnerEtKey(Ctx, Ct);
		var UpdById = await DaoKv.FnUpdById(Ctx, Ct);
		var InsertMany = await RepoCfg.FnInsertMany(Ctx, Ct);
		return async(UserId, Key, Po, Ct)=>{
			var Existing = await GetByOwnerEtKey(UserId, Key, Ct);
			if(Existing is not null){
				await UpdById(Existing.Id, Po, Ct);
				return NIL;
			}
			Po.Owner = UserId;
			await InsertMany([Po], Ct);
			return NIL;
		};
	}




	[Obsolete]
	[Impl(typeof(ISvcKv))]
	public async Task<PoKv?> GetByKStr(IUserCtx UserCtx, str Key, CT Ct){
		return await TxnWrapper.Wrap(FnGetOneByKStr, UserCtx, Key, Ct);
		//return await TxnWrapper.Wrap(new ClsGetOneByKStr(this), UserCtx, Key, Ct);
	}
	[Obsolete]
	[Impl(typeof(ISvcKv))]
	public async Task<nil> SetVStrByKStr(IUserCtx UserCtx, str Key, str Value, CT Ct){
		return await TxnWrapper.Wrap(FnAddOrSetVStrByKStr, UserCtx, Key, Value, Ct);
	}
	[Obsolete]
	[Impl(typeof(ISvcKv))]
	public async Task<nil> SetVI64ByKStr(IUserCtx UserCtx, str Key, i64 Value, CT Ct){
		return await TxnWrapper.Wrap(FnAddOrSetVI64ByKStr, UserCtx, Key, Value, Ct);
	}


	[Obsolete]
	public async Task<Func<
		IUserCtx
		,str
		,CT
		,Task<PoKv?>
	>> FnGetOneByKStr(IDbFnCtx Ctx, CT Ct){
		var PageQry = new PageQry{PageIdx = 0, PageSize = 1, WantTotCnt = false};
		var PageByKStr = await DaoKv.FnPageByKStr(Ctx, Ct);
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

	[Obsolete]
	public async Task<Func<
		IUserCtx
		,PoKv
		,CT, Task<nil>
	>> FnUpdByKey(IDbFnCtx Ctx, CT Ct){
		var UpdById = await DaoKv.FnUpdById(Ctx, Ct);
		return async(User, Po, Ct)=>{

			return NIL;
		};
	}

	[Obsolete]
	public async Task<Func<
		IUserCtx
		,PoKv
		,CT
		,Task<nil>
	>> FnAdd(IDbFnCtx Ctx, CT Ct){
		var InsertMany = await RepoCfg.FnInsertMany(Ctx, Ct);
		var Fn = async(IUserCtx UserCtx, PoKv PoCfg, CT Ct)=>{
			PoCfg.Owner = UserCtx.UserId;
			await InsertMany([PoCfg], Ct);
			return NIL;
		};
		return Fn;
	}


	[Obsolete]
	public async Task<Func<
		IUserCtx
		,str
		,str
		,CT
		,Task<nil>
	>> FnAddOrSetVStrByKStr(IDbFnCtx Ctx, CT Ct){
		var GetOneByKStr = await FnGetOneByKStr(Ctx, Ct);
		var Add = await FnAdd(Ctx, Ct);
		var SetVStrByKStr = await DaoKv.FnSetVStrByKStr(Ctx, Ct);
		var Fn = async(IUserCtx UserCtx, str Key, str Value, CT Ct)=>{
			var Existing = await GetOneByKStr(UserCtx, Key, Ct);
			if(Existing != null){
				await SetVStrByKStr(UserCtx, Key, Value, Ct);
			}else{
				var ToAdd = new PoKv{
					Owner = UserCtx.UserId,
					KStr = Key,
					VType = EKvType.Str,
					VStr = Value,
				};
				await Add(UserCtx, ToAdd, Ct);
			}
			return NIL;
		};
		return Fn;
	}

	[Obsolete]
	public async Task<Func<
		IUserCtx
		,str
		,i64
		,CT
		,Task<nil>
	>> FnAddOrSetVI64ByKStr(IDbFnCtx Ctx, CT Ct){
		var GetOneByKStr = await FnGetOneByKStr(Ctx, Ct);
		var Add = await FnAdd(Ctx, Ct);
		var SetVI64ByKStr = await DaoKv.FnSetVI64ByKStr(Ctx, Ct);
		var Fn = async(IUserCtx UserCtx, str Key, i64 Value, CT Ct)=>{
			var Existing = await GetOneByKStr(UserCtx, Key, Ct);
			if(Existing != null){
				await SetVI64ByKStr(UserCtx, Key, Value, Ct);
			}else{
				var ToAdd = new PoKv{
					Owner = UserCtx.UserId,
					KStr = Key,
					VType = EKvType.I64,
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
