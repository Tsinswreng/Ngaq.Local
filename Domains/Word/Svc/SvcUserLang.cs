using Ngaq.Core.Infra;
using Ngaq.Core.Infra.Errors;
using Ngaq.Core.Shared.Base.Models.Po;
using Ngaq.Core.Shared.Dictionary.Models;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.Word.Models.Dto;
using Ngaq.Core.Shared.Word.Models.Po.UserLang;
using Ngaq.Core.Shared.Word.Svc;
using Ngaq.Local.Db.TswG;
using Ngaq.Local.Domains.Word.Dao;
using Tsinswreng.CsErr;
using Tsinswreng.CsPage;
using Tsinswreng.CsSql;
using Tsinswreng.CsTools;

namespace Ngaq.Local.Domains.Word.Svc;

public class SvcUserLang : ISvcUserLang {
	private readonly DaoUserLang Dao;
	private readonly ISqlCmdMkr SqlCmdMkr;
	private readonly IRepo<PoUserLang, IdUserLang> RepoUserLang;

	public SvcUserLang(DaoUserLang dao, ISqlCmdMkr sqlCmdMkr, IRepo<PoUserLang, IdUserLang> repoUserLang) {
		Dao = dao;
		SqlCmdMkr = sqlCmdMkr;
		RepoUserLang = repoUserLang;
	}

	// 只读操作保持不变（仅初始化 DbFnCtx）
	public async Task<IPageAsyE<PoUserLang>> PageUserLang(IDbUserCtx Ctx, ReqPageUserLang Req, CT Ct) {
		Ctx.DbFnCtx ??= new DbFnCtx();
		return await Dao.PageUserLang(Ctx.DbFnCtx, Ctx.UserCtx.UserId, Req, Ct);
	}

	/// 批量按語言唯一名讀取當前用戶的語言實體。
	/// 若某個唯一名不存在，對應位置返回 null。
	/// <param name="Ctx">用戶+數據庫上下文。</param>
	/// <param name="UniqNames">待查語言唯一名序列。</param>
	/// <param name="Ct">取消令牌。</param>
	/// <returns>與輸入對齊的 <see cref="PoUserLang"/> 可空序列。</returns>
	public IAsyncEnumerable<PoUserLang?> BatGetUserLang(
		IDbUserCtx Ctx,
		IAsyncEnumerable<str> UniqNames,
		CT Ct
	){
		Ctx.DbFnCtx ??= new DbFnCtx();
		return Dao.BatGetUserLang(Ctx.DbFnCtx, Ctx.UserCtx.UserId, UniqNames, Ct);
	}

	public IAsyncEnumerable<str> GetUnregisteredUserLangs(IDbUserCtx Ctx, CT Ct) {
		Ctx.DbFnCtx ??= new DbFnCtx();
		return Dao.GetUnregisteredUserLangs(Ctx.DbFnCtx, Ctx.UserCtx.UserId, Ct);
	}

	// 写操作统一通过 ExecuteWriteOperation 执行
	public async Task<nil> BatUpdUserLang(IDbUserCtx Ctx, IAsyncEnumerable<PoUserLang> Pos, CT Ct) =>
		await DoWrite(Ctx, Pos, needTouch: false, async (dbCtx, repo, pos, ct) => {
			await repo.BatUpd(dbCtx, pos, ct);
			await repo.AsAppRepo().BatBizTouch(dbCtx, pos.Select(x => x.Id), ct);
		}, Ct);

	public async Task<nil> BatAddUserLang(IDbUserCtx Ctx, IAsyncEnumerable<PoUserLang> Pos, CT Ct) =>
		await DoWrite(Ctx, Pos, needTouch: true, async (dbCtx, repo, pos, ct) => {
			await repo.BatAdd(dbCtx, pos, ct);
		}, Ct);

	public async Task<nil> AddAllUnregisteredUserLangs(IDbUserCtx Ctx, CT Ct) {
		Ctx.DbFnCtx??=new DbFnCtx();
		var langs = Dao.GetUnregisteredUserLangs(Ctx.DbFnCtx, Ctx.UserCtx.UserId, Ct);
		var pos = langs.Select(lang => new PoUserLang {
			Owner = Ctx.UserCtx.UserId,
			UniqName = lang,
			RelLangType = ELangIdentType.Bcp47,
			RelLang = "",
			Descr = "",
		});
		await DoWrite(Ctx, pos, needTouch: true, async (dbCtx, repo, pos, ct) => {
			await repo.BatAdd(dbCtx, pos, ct);
		}, Ct);
		return NIL;
	}

	// 私有模板方法：统一处理权限、Touch、事务和异常转换
	private async Task<nil> DoWrite(
		IDbUserCtx ctx,
		IAsyncEnumerable<PoUserLang> pos,
		bool needTouch,
		Func<IDbFnCtx, IRepo<PoUserLang, IdUserLang>, IAsyncEnumerable<PoUserLang>, CT, Task> operation,
		CT ct) {
		// 1. 强制所有权检查（若任意实体 owner 不匹配，CheckOwner 内部会抛出 PermissionDenied）
		pos = pos.CheckOwner(ctx.UserCtx.UserId);

		// 2. 可选的时间戳设置
		if (needTouch)
			pos = pos.Touch();

		try {
			// 3. 事务管理（若已有 DbFnCtx 则复用，否则开启新事务）
			await SqlCmdMkr.EnsureTxn(ctx.DbFnCtx, ct, async txnCtx => {
				await operation(txnCtx, RepoUserLang, pos, ct);
				return NIL;
			});
			return NIL;
		}
		catch (AppErr){
			throw;
		}
		catch (Exception ex) {
			// 其他异常统一包装为 DataIllegalOrConflict
			var e = ItemsErr.Common.DataIllegalOrConflict.ToErr();
			e.AddErr(ex);
			throw e;
		}
	}
}
