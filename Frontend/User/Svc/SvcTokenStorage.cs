namespace Ngaq.Backend.Frontend.User.Svc;

using Ngaq.Core.Frontend.Kv;
using Ngaq.Core.Frontend.User.Svc;
using Ngaq.Core.Infra.IF;
using Ngaq.Core.Shared.Kv.Models;
using Ngaq.Core.Shared.Kv.Svc;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Tsinswreng.CsCore;
using Tsinswreng.CsSql;
using Tsinswreng.CsTools;

public class SvcTokenStorage:ISvcTokenStorage{
	//public ISvcSecretKv SvcSecretKv;
	public ISvcKv SvcKv;
	public ISqlCmdMkr SqlCmdMkr;

	/// <summary>
	/// 在同一個數據庫上下文中先查舊行再沿用 Id 寫回，
	/// 避免同一組客戶端鍵被當成新行插入；
	/// 同時保證 sqlite 單例連接下的查寫共用同一個事務邊界。
	/// </summary>
	/// <param name="Ctx">當前數據庫上下文；需與最終寫回共用。</param>
	/// <param name="Key">客戶端鍵名。</param>
	/// <param name="Kv">準備寫回的資料列。</param>
	/// <param name="Ct">取消令牌。</param>
	/// <returns>可直接交給 <see cref="ISvcKv.BatSet"/> 的資料列。</returns>
	async Task<PoKv> MkUpsertKv(
		IDbFnCtx Ctx
		,str Key
		,PoKv Kv
		,CT Ct
	){
		// 查舊值與後續 BatSet 必須共用同一個 Ctx；
		// 否則在 sqlite 單例連接上容易出現 pending local transaction 狀態污染。
		var OldKv = await SvcKv.BatGetByOwnerEtKStr(
			Ctx,
			ToolAsyE.ToAsyE([(IdUser.Zero, Key)]),
			Ct
		).FirstOrDefaultAsync(Ct);
		if(OldKv is not null){
			Kv.Id = OldKv.Id;
		}
		return Kv;
	}

	public SvcTokenStorage(
		//ISvcSecretKv SvcSecretKv
		ISvcKv SvcKv
		,ISqlCmdMkr SqlCmdMkr
	){
		//this.SvcSecretKv = SvcSecretKv;
		this.SvcKv = SvcKv;
		this.SqlCmdMkr = SqlCmdMkr;
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

	[Impl]
	public async Task<nil> SetRefreshToken(ReqSetRefreshToken Req, CT Ct){
		// 三個客戶端鍵要麼一起成功，要麼一起失敗；
		// 因此顯式收進同一個 Ctx/Txn 中，既符合業務一致性，也避免 sqlite 連接狀態污染。
		return await SqlCmdMkr.EnsureTxn(null, Ct, async(Ctx)=>{
			var RefreshTokenKv = await MkUpsertKv(
				Ctx,
				KeysClientKv.RefreshToken+"",
				new PoKv{
					Owner = IdUser.Zero,
				}.SetStrStr(KeysClientKv.RefreshToken, Req.RefreshToken),
				Ct
			);
			var RefreshTokenExpireAtKv = await MkUpsertKv(
				Ctx,
				KeysClientKv.RefreshTokenExpireAt+"",
				new PoKv{
					Owner = IdUser.Zero,
				}.SetStrI64(KeysClientKv.RefreshTokenExpireAt, Req.RefreshTokenExpireAt),
				Ct
			);
			var CurLoginUserIdKv = await MkUpsertKv(
				Ctx,
				KeysClientKv.CurLoginUserId+"",
				new PoKv{Owner = IdUser.Zero}
					.SetStrStr(KeysClientKv.CurLoginUserId, Req.LoginUserId+""),
				Ct
			);
			await SvcKv.BatSet(
				Ctx,
				ToolAsyE.ToAsyE(
					[
						RefreshTokenKv,
						RefreshTokenExpireAtKv,
						CurLoginUserIdKv
					]
				),
				Ct
			);
			return NIL;
		});
	}

}
