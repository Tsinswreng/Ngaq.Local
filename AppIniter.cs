namespace Ngaq.Local;

using Ngaq.Core.Shared.Kv.Models;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Frontend.Kv;
using Ngaq.Local.Sql;
using Ngaq.Core.Shared.Kv.Svc;
using Ngaq.Core.Frontend.User;
using Ngaq.Core.Shared.User.Models.Po.Device;
using Microsoft.Extensions.DependencyInjection;

// public interface IDependencyGetter{
// 	public T GetSvc<T>()where T : class;
// }

// public class DependencyGetter(
// 	IServiceProvider SvcProvider
// ) : IDependencyGetter{

// 	public T GetSvc<T>()
// 		where T : class
// 	{
// 		return SvcProvider.GetRequiredService<T>();
// 	}
// }

public static class ExtnIServiceProvider{
	public static T GetSvc<T>(this IServiceProvider z)
		where T : class
	{
		return z.GetRequiredService<T>();
	}
}

/// <summary>
/// 注意: 需動上初始化 DependencyGetter
/// </summary>
public class AppIniter{
	protected static AppIniter? _Inst = null;
	public static AppIniter Inst => _Inst??= new AppIniter();
	public IServiceProvider SvcProvider{get;set;} = null!;

	public async Task<nil> Init(CT Ct){
		if(SvcProvider is null){
			throw new InvalidOperationException("SvcMgr is null. Make sure DependencyGetter is initialized manually.");
		}
		await InitDbSchema(Ct);
		await InitUserCtx(Ct);
		return NIL;
	}


	async Task<IdClient> InitClientId(CT Ct){
		var SvcKv = SvcProvider.GetSvc<ISvcKv>();
		var Key = KeysClientKv.ClientId;
		var CliendIdKv = await SvcKv.GetByOwnerEtKeyAsy(
			IdUser.Zero, Key, Ct
		);


		if(CliendIdKv is null){
			var Id = new IdClient();
			await SvcKv.SetAsy(new PoKv{
				Owner = IdUser.Zero
			}.SetStrStr(Key, Id+""), Ct);
			return Id;
		}
		return IdClient.FromLow64Base(
			CliendIdKv.GetVStr()??throw new InvalidOperationException("Invalid Client Id")
		);

	}

	public async Task<nil> InitUserCtx(CT Ct){
		var userCtxMgr = SvcProvider.GetSvc<IFrontendUserCtxMgr>();
		var SvcKv = SvcProvider.GetSvc<ISvcKv>();

		var CurLocalUserKv = await SvcKv.GetByOwnerEtKeyAsy(IdUser.Zero,KeysClientKv.CurLocalUserId,Ct);
		var CurLoginUserKv = await SvcKv.GetByOwnerEtKeyAsy(IdUser.Zero,KeysClientKv.CurLoginUserId,Ct);
		var RefreshToken = await SvcKv.GetByOwnerEtKeyAsy(IdUser.Zero, KeysClientKv.RefreshToken, Ct);
		var RefreshTokenExpireAt = await SvcKv.GetByOwnerEtKeyAsy(IdUser.Zero, KeysClientKv.RefreshTokenExpireAt, Ct);
		var UserCtx = userCtxMgr.GetUserCtx();
		if(RefreshToken is not null){//TODO 判段是否過期
			UserCtx.RefreshToken = RefreshToken.GetVStr();
			UserCtx.RefreshTokenExpireAt = RefreshTokenExpireAt?.GetVI64()??0;
		}
		if(CurLoginUserKv is not null){//TODO 判段是否過期
			var LoginUserId = IdUser.FromLow64Base(
				CurLoginUserKv.VStr??throw new InvalidOperationException("Invalid User Id")
			);
			UserCtx.LoginUserId = LoginUserId;
		}

		if(CurLocalUserKv is not null){
			var LocalUserId = IdUser.FromLow64Base(
				CurLocalUserKv.VStr??throw new InvalidOperationException("Invalid User Id")
			);
			UserCtx.UserId = LocalUserId; // deprecated
			UserCtx.LocalUserId = LocalUserId;
		}else{
			var kv = new PoKv();
			var LocalUserId = new IdUser();
			UserCtx.UserId = LocalUserId;
			UserCtx.LocalUserId = LocalUserId;
			kv.SetStrStr(KeysClientKv.CurLocalUserId, userCtxMgr.GetUserCtx().UserId.ToString());
			await SvcKv.SetAsy(
				kv, Ct
			);
		}
		UserCtx.ClientId = await InitClientId(Ct);
		return NIL;
	}

	public async Task<nil> InitDbSchema(CT Ct){
		var DbIniter = SvcProvider.GetSvc<DbIniter>();
		_ = DbIniter.Init(Ct).Result;
		return NIL;
	}
}
