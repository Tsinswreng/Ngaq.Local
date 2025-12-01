namespace Ngaq.Local.Frontend.User.Svc;

using Ngaq.Core.Frontend.Kv;
using Ngaq.Core.Frontend.User.Svc;
using Ngaq.Core.Shared.Kv.Models;
using Ngaq.Core.Shared.Kv.Svc;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Tsinswreng.CsCore;

public class SvcTokenStorage:ISvcTokenStorage{
	//public ISvcSecretKv SvcSecretKv;
	public ISvcKv SvcKv;
	public SvcTokenStorage(
		//ISvcSecretKv SvcSecretKv
		ISvcKv SvcKv
	){
		//this.SvcSecretKv = SvcSecretKv;
		this.SvcKv = SvcKv;
	}

	[Impl]
	public async Task<str?> GetRefreshToken(CT Ct){
		//TODO 先直接存明文 後汶改加密
		var kv = await SvcKv.GetByOwnerEtKeyAsy(
			IdUser.Zero, KeysClientKv.RefreshToken, Ct
		);
		return kv?.GetVStr();
	}

	[Obsolete]
	[Impl]
	public async Task<nil> SetRefreshToken(str Token, CT Ct){
		//TODO 先直接存明文 後汶改加密
		await SvcKv.SetAsy(
			new PoKv{
				Owner = IdUser.Zero,
			}.SetStrStr(KeysClientKv.RefreshToken, Token)
			,Ct
		);
		return NIL;
	}

	[Impl]
	public async Task<nil> SetRefreshToken(ReqSetRefreshToken Req, CT Ct){
		await SvcKv.SetManyAsy(
			[
				new PoKv{
					Owner = IdUser.Zero,
				}.SetStrStr(KeysClientKv.RefreshToken, Req.RefreshToken)
				,new PoKv{
					Owner = IdUser.Zero,
				}.SetStrI64(KeysClientKv.RefreshTokenExpireAt, Req.RefreshTokenExpireAt)
				,new PoKv{Owner = IdUser.Zero}
				.SetStrStr(KeysClientKv.CurLoginUserId, Req.LoginUserId+"")
			],Ct
		);
		return NIL;
	}

}
