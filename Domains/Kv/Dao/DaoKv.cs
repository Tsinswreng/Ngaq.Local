namespace Ngaq.Local.Domains.Kv.Dao;

using Ngaq.Core.Shared.Kv.Models;
using Ngaq.Core.Shared.User.Models;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.User.UserCtx;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Sys.Models;
using Ngaq.Local.Db.TswG;
using Tsinswreng.CsCfg;
using Tsinswreng.CsCore;
using Tsinswreng.CsPage;
using Tsinswreng.CsSqlHelper;
using Tsinswreng.CsTools;


using Z = DaoKv;
public partial class DaoKv(
	ISqlCmdMkr SqlCmdMkr
	,ITblMgr TblMgr
	,IAppRepo<PoKv, IdKv> RepoKv
){

	ISqlCmdMkr SqlCmdMkr = SqlCmdMkr;

	protected ITable T{
		get{return TblMgr.GetTbl<PoKv>();}
	}

	public async Task<Func<
		PoKv
		,CT, Task<nil>
	>> FnUpdById(IDbFnCtx Ctx, CT Ct){
		var UpdById = await RepoKv.FnUpdOneById(Ctx,null, Ct);
		return async(Po, Ct)=>{
			await UpdById(Po, Ct);
			return NIL;
		};
	}

	public async Task<Func<
		IEnumerable<PoKv>
		,CT, Task<nil>
	>> FnUpdManyById(IDbFnCtx Ctx, CT Ct){
		var UpdById = await RepoKv.FnUpdManyById(Ctx, null, Ct);
		return async(Po, Ct)=>{
			await UpdById(Po, Ct);
			return NIL;
		};
	}

	async Task<Func<
		IdUser
		,obj
		,CT, Task<PoKv?>
	>> FnGetByOwnerEtKey(IDbFnCtx Ctx, str KeyCol, CT Ct){
var T = TblMgr.GetTbl<PoKv>(); var POwner = T.Prm(nameof(PoKv.Owner)); var PKeyCol = T.Prm(KeyCol);
var Sql =$"""
SELECT * FROM {T.Qt(T.DbTblName)}
WHERE 1=1
AND {T.SqlIsNonDel()}
AND {T.Eq(POwner)}
AND {T.Eq(PKeyCol)}
""";
		var SqlCmd = await SqlCmdMkr.Prepare(Ctx, Sql, Ct);
		Ctx?.AddToDispose(SqlCmd);
		return async(Id, Key, Ct)=>{
			var Arg = ArgDict.Mk(T).AddT(POwner, Id).AddT(PKeyCol, Key);
			return await SqlCmd.Args(Arg).FirstOrDefault<PoKv>(T, Ct);
		};
	}

	public async Task<Func<
		IdUser
		,str
		,CT, Task<PoKv?>
	>> FnGetByOwnerEtKStr(IDbFnCtx Ctx, CT Ct){
		return await FnGetByOwnerEtKey(Ctx, nameof(PoKv.KStr), Ct);
	}

	public async Task<Func<
		IdUser
		,i64
		,CT, Task<PoKv?>
	>> FnGetByOwnerEtKI64(IDbFnCtx Ctx, CT Ct){
		var Fn = await FnGetByOwnerEtKey(Ctx, nameof(PoKv.KI64), Ct);
		return async(Id, Key, Ct)=>{
			var R = await Fn(Id, Key, Ct);
			return R;
		};
	}


	[Obsolete]
	public async Task<Func<
		IUserCtx
		,str
		,IPageQry
		,CT
		,Task<IPage<PoKv>>
	>> FnPageByKStr(IDbFnCtx Ctx, CT Ct){
var NOwner = nameof(PoKv.Owner); var NKStr = nameof(PoKv.KStr);
var POwner = T.Prm(NOwner);var PKStr = T.Prm(NKStr);
var Sql = $"""
SELECT * FROM {T.Qt(T.DbTblName)}
WHERE {T.Qt(NOwner)} = {POwner}
AND {T.Qt(NKStr)} = {PKStr}
{T.SqlMkr.ParamLimOfst(out var Lmt, out var Ofst)}
""";
		var SqlCmd = await SqlCmdMkr.Prepare(Ctx, Sql, Ct);
		Ctx?.AddToDispose(SqlCmd);
		var Fn = async(IUserCtx User, str KStr, IPageQry PageQry, CT Ct)=>{
			var Arg = ArgDict.Mk(T)
			.AddT(POwner, User.UserId)
			.AddT(PKStr, KStr)
			.AddPageQry(PageQry, Lmt, Ofst);
			var RawDicts = await SqlCmd.Args(Arg).All(Ct);
			var PoAsy = RawDicts.Select(R=>T.DbDictToEntity<PoKv>(R)).ToListTryNoCopy();
			var R = Page.Mk(PageQry, PoAsy);
			return R;
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
	>> FnSetVStrByKStr(IDbFnCtx Ctx, CT Ct){
var NVStr = nameof(PoKv.VStr); var NOwner = nameof(PoKv.Owner); var NKStr = nameof(PoKv.KStr);
var PVStr = T.Prm(NVStr);var POwner = T.Prm(NOwner); var PKStr = T.Prm(NKStr);
var Sql = $"""
UPDATE {T.Qt(T.DbTblName)}
SET {T.Qt(NVStr)} = {PVStr}
WHERE {T.Qt(NOwner)} = {POwner}
AND {T.Qt(NKStr)} = {PKStr}
""";
var SqlCmd = await SqlCmdMkr.Prepare(Ctx, Sql, Ct);
		Ctx?.AddToDispose(SqlCmd);
		var Fn = async(IUserCtx User, str KStr, str VStr, CT Ct)=>{
			var Args = ArgDict.Mk()
			.AddRaw(POwner, T.UpperToRaw(User.UserId))
			.AddRaw(PKStr, KStr).AddRaw(PVStr, VStr);
			await SqlCmd.Args(Args).IterAsyE(Ct).FirstOrDefaultAsync(Ct);
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
	>> FnSetVI64ByKStr(IDbFnCtx Ctx, CT Ct){
var NVI64 = nameof(PoKv.VI64); var NOwner = nameof(PoKv.Owner); var NKStr = nameof(PoKv.KStr);
var PI64 = T.Prm(NVI64);var POwner = T.Prm(NOwner); var PKStr = T.Prm(NKStr);
var Sql = $"""
UPDATE {T.Qt(T.DbTblName)}
SET {T.Qt(NVI64)} = {PI64}
WHERE {T.Qt(NOwner)} = {POwner}
AND {T.Qt(NKStr)} = {PKStr}
""";
var SqlCmd = await SqlCmdMkr.Prepare(Ctx, Sql, Ct);
		Ctx?.AddToDispose(SqlCmd);
		var Fn = async(IUserCtx User, str KStr, i64 VI64, CT Ct)=>{
			var Args = ArgDict.Mk()
			.AddRaw(POwner, T.UpperToRaw(User.UserId))
			.AddRaw(PKStr, KStr).AddRaw(PI64, VI64);
			await SqlCmd.Args(Args).IterAsyE(Ct).FirstOrDefaultAsync(Ct);
			return NIL;
		};
		return Fn;
	}


#if false
public class ClsPageByKStr(Z z):DbFn{
		public Func<IUserCtx,str,IPageQuery,CT,Task<IPageAsy<PoCfg>>> FnRun=null!;
		public async Task<ClsPageByKStr> Init(IDbFnCtx Ctx, CT Ct){
			var T = z.T;
			var NOwner = nameof(PoCfg.Owner); var NKStr = nameof(PoCfg.KStr);
			var POwner = T.Prm(NOwner);var PKStr = T.Prm(NKStr);
			var Sql = $"""
			SELECT * FROM {T.Qt(T.DbTblName)}
			WHERE {T.Qt(NOwner)} = {POwner}
			AND {T.Qt(NKStr)} = {PKStr}
			{T.SqlMkr.PrmLmtOfst(out var Lmt, out var Ofst)}
			""";
			SqlCmd = await z.SqlCmdMkr.Prepare(Ctx, Sql, Ct);
			FnRun = async(IUserCtx User, str KStr, IPageQuery PageQry, CT Ct)=>{
				var Arg = ArgDict.Mk()
				.Add(POwner, T.UpperToRaw(User.UserId))
				.Add(PKStr, KStr)
				.AddPageQry(PageQry, Lmt, Ofst);
				var RawDicts = SqlCmd.Args(Arg).Run(Ct);
				var PoAsy = RawDicts.Select(R=>T.DbDictToPo<PoCfg>(R));
				var R = PageAsy.Mk(PageQry, PoAsy);
				return R;
			};
			return this;
		}
	}
#endif

#if false
	public class ClsSetVStrByKStr(Z z):DbFn{
		public Func<IUserCtx,str,str,CT,Task<nil>> FnRun = null!;
		public async Task Init(CT Ct){
			var T = z.T;
			var NVStr = nameof(PoCfg.VStr); var NOwner = nameof(PoCfg.Owner); var NKStr = nameof(PoCfg.KStr);
			var PVStr = T.Prm(NVStr);var POwner = T.Prm(NOwner); var PKStr = T.Prm(NKStr);
			var Sql = $"""
			UPDATE {T.Qt(T.DbTblName)}
			SET {T.Qt(NVStr)} = {PVStr}
			WHERE {T.Qt(NOwner)} = {POwner}
			AND {T.Qt(NKStr)} = {PKStr}
			""";
			var SqlCmd = await z.SqlCmdMkr.Prepare(Ctx, Sql, Ct);
			FnRun = async(IUserCtx User, str KStr, str VStr, CT Ct)=>{
				var Args = ArgDict.Mk()
				.Add(POwner, T.UpperToRaw(User.UserId))
				.Add(PKStr, KStr).Add(PVStr, VStr);
				await SqlCmd.Args(Args).Run(Ct).FirstOrDefaultAsync(Ct);
				return NIL;
			};
		}
	}


	public class ClsSetVI64ByKStr(Z z):DbFn{
		public Func<IUserCtx, str, i64, CT, Task<nil>> FnRun=null!;
		public async Task Init(CT Ct){
			var T = z.T;
			str NVI64 = nameof(PoCfg.VI64), NOwner = nameof(PoCfg.Owner), NKStr = nameof(PoCfg.KStr);
			IParam PI64 = z.T.Prm(NVI64), POwner = T.Prm(NOwner), PKStr = T.Prm(NKStr);
			var Sql = $"""
			UPDATE {T.Qt(T.DbTblName)}
			SET {T.Qt(NVI64)} = {PI64}
			WHERE {T.Qt(NOwner)} = {POwner}
			AND {T.Qt(NKStr)} = {PKStr}
			""";
			this.SqlCmd = await z.SqlCmdMkr.Prepare(Ctx, Sql, Ct);
			FnRun = async(IUserCtx User, str KStr, i64 VI64, CT Ct)=>{
				var Args = ArgDict.Mk()
				.Add(POwner, T.UpperToRaw(User.UserId))
				.Add(PKStr, KStr).Add(PI64, VI64);
				await SqlCmd.Args(Args).Run(Ct).FirstOrDefaultAsync(Ct);
				return NIL;
			};
		}
	}
#endif
}
