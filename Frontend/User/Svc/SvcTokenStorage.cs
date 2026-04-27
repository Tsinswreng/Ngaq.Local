namespace Ngaq.Backend.Frontend.User.Svc;

using Ngaq.Core.Frontend.Kv;
using Ngaq.Core.Frontend.User.Svc;
using Ngaq.Core.Infra.IF;
using Ngaq.Core.Shared.Kv.Models;
using Ngaq.Core.Shared.Kv.Svc;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Tsinswreng.CsCore;
using Tsinswreng.CsTools;

public class SvcTokenStorage:ISvcTokenStorage{
	//public ISvcSecretKv SvcSecretKv;
	public ISvcKv SvcKv;

	/// <summary>
	/// 先查舊行再沿用 Id 寫回，避免同一組客戶端鍵被當成新行插入。
	/// </summary>
	/// <param name="Key">客戶端鍵名。</param>
	/// <param name="Kv">準備寫回的資料列。</param>
	/// <param name="Ct">取消令牌。</param>
	/// <returns>可直接交給 <see cref="ISvcKv.BatSet"/> 的資料列。</returns>
	async Task<PoKv> MkUpsertKv(str Key, PoKv Kv, CT Ct){
		var OldKv = await SvcKv.GetByOwnerEtKStr(IdUser.Zero, Key, Ct);
		if(OldKv is not null){
			Kv.Id = OldKv.Id;
		}
		return Kv;
	}

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
		var kv = SvcKv.BatGetByOwnerEtKStr(
			null, ToolAsyE.ToAsyE([(IdUser.Zero, KeysClientKv.RefreshToken+"")]), Ct
		);
		var first = await kv.FirstOrDefaultAsync(Ct);
		return first?.GetVStr();
	}

	[Obsolete]
	[Impl]
	public async Task<nil> SetRefreshToken(str Token, CT Ct){
		//TODO 先直接存明文 後汶改加密
		var RefreshTokenKv = await MkUpsertKv(
			KeysClientKv.RefreshToken+""
			,new PoKv{
				Owner = IdUser.Zero,
			}.SetStrStr(KeysClientKv.RefreshToken, Token)
			,Ct
		);
		await SvcKv.BatSet(
			null,
			ToolAsyE.ToAsyE([RefreshTokenKv])
			,Ct
		);
		return NIL;
	}

	[Impl]
	public async Task<nil> SetRefreshToken(ReqSetRefreshToken Req, CT Ct){
		var RefreshTokenKv = await MkUpsertKv(
			KeysClientKv.RefreshToken+""
			,new PoKv{
				Owner = IdUser.Zero,
			}.SetStrStr(KeysClientKv.RefreshToken, Req.RefreshToken)
			,Ct
		);
		var RefreshTokenExpireAtKv = await MkUpsertKv(
			KeysClientKv.RefreshTokenExpireAt+""
			,new PoKv{
				Owner = IdUser.Zero,
			}.SetStrI64(KeysClientKv.RefreshTokenExpireAt, Req.RefreshTokenExpireAt)
			,Ct
		);
		var CurLoginUserIdKv = await MkUpsertKv(
			KeysClientKv.CurLoginUserId+""
			,new PoKv{Owner = IdUser.Zero}
				.SetStrStr(KeysClientKv.CurLoginUserId, Req.LoginUserId+"")
			,Ct
		);
		await SvcKv.BatSet(
			null,
			ToolAsyE.ToAsyE(
				[
					RefreshTokenKv
					,RefreshTokenExpireAtKv
					,CurLoginUserIdKv
				]
			)
			,Ct
		);
		return NIL;
	}

}
