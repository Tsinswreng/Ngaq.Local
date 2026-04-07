namespace Ngaq.Local.Domains.Kv.Svc;

using Ngaq.Core.Shared.Kv.Models;
using Ngaq.Core.Shared.Kv.Svc;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Local.Db.TswG;
using Ngaq.Local.Domains.Kv.Dao;
using Tsinswreng.CsCore;
using Tsinswreng.CsSql;

using Z = SvcKv;
public partial class SvcKv{

	public Task<PoKv?> GetByOwnerEtKStr(IdUser Owner, obj Key, CT Ct){
		return TxnWrapper.Wrap(FnGetByOwnerEtKey, Owner, Key, Ct);
	}

	public Task<nil> Set(PoKv Po, CT Ct){
		return TxnWrapper.Wrap(FnSet, Po, Ct);
	}

	public Task<nil> SetMany(IEnumerable<PoKv> Pos, CT Ct){
		return TxnWrapper.Wrap(FnSetMany, Pos, Ct);
	}


	public async Task<Func<
		IdUser
		,obj
		,CT, Task<PoKv?>
	>> FnGetByOwnerEtKey(IDbFnCtx Ctx, CT Ct){
		var GetByKStr = await DaoKv.FnGetByOwnerEtKStr(Ctx, Ct);
		var GetByKI64 = await DaoKv.FnGetByOwnerEtKI64(Ctx, Ct);
		return async(Owner, Key, Ct)=>{
			if(Key is i64 KI64){
				PoKv? R = await GetByKI64(Owner, KI64, Ct);
				return R;
			}else{
				PoKv? R = await GetByKStr(Owner, Key+"", Ct);
				return R;
			}
		};
	}

	public async Task<Func<
		PoKv
		,CT, Task<nil>
	>> FnSet(IDbFnCtx Ctx, CT Ct){
		var GetByOwnerEtKey = await FnGetByOwnerEtKey(Ctx, Ct);
		var UpdById = await DaoKv.FnUpdById(Ctx, Ct);
		var InsertMany = await RepoKv.FnInsertMany(Ctx, Ct);
		return async(Po, Ct)=>{
			var UserId = Po.Owner;
			obj Key = null!;
			if(Po.KType == EKvType.Str){
				Key = Po.KStr!;
			}else{
				Key = Po.KI64;
			}
			var Existing = await GetByOwnerEtKey(UserId, Key, Ct);
			if(Existing is not null){
				await UpdById(Po, Ct);
				return NIL;
			}
			await InsertMany([Po], Ct);
			return NIL;
		};
	}


	public async Task<Func<
		IEnumerable<PoKv>
		,CT, Task<nil>
	>> FnSetMany(IDbFnCtx Ctx, CT Ct){
		var GetByOwnerEtKey = await FnGetByOwnerEtKey(Ctx, Ct);
		var UpdManyById = await DaoKv.FnUpdManyById(Ctx, Ct);
		var InsertMany = await RepoKv.FnInsertMany(Ctx, Ct);
		return async(Pos, Ct)=>{
			List<PoKv> Existings = new List<PoKv>();
			List<PoKv> NonExistings = new List<PoKv>();
			foreach(var Po in Pos){
				var Existing = await GetByOwnerEtKey(Po.Owner, Po.GetKey(), Ct);
				if(Existing is not null){
					Po.Id = Existing.Id;
					Existings.Add(Po);
				}else{
					NonExistings.Add(Po);
				}
			}
			await UpdManyById(Existings, Ct);
			await InsertMany(NonExistings, Ct);
			return NIL;
		};
	}
}
