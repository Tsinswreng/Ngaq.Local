using Ngaq.Core.Model.UserCtx;
using Ngaq.Core.Models.UserCtx;
using Ngaq.Core.Sys.Models;
using Ngaq.Local.Db;
using Ngaq.Local.Db.TswG;
using Tsinswreng.CsCfg;
using Tsinswreng.CsCore;
using Tsinswreng.CsPage;
using Tsinswreng.CsSqlHelper;

namespace Ngaq.Local.Sys.Dao;
using Z = DaoCfg;
public partial class DaoCfg(
	ISqlCmdMkr SqlCmdMkr
	,ITblMgr TblMgr
	,IAppRepo<PoCfg, IdCfg> RepoCfg
){

	ISqlCmdMkr SqlCmdMkr = SqlCmdMkr;

	protected ITable T{
		get{return TblMgr.GetTbl<PoCfg>();}
	}


	public async Task<Func<
		IUserCtx
		,str
		,IPageQry
		,CT
		,Task<IPageAsy<PoCfg>>
	>> FnPageByKStr(IDbFnCtx Ctx, CT Ct){
var NOwner = nameof(PoCfg.Owner); var NKStr = nameof(PoCfg.KStr);
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
			var Arg = ArgDict.Mk()
			.Add(POwner, T.UpperToRaw(User.UserId))
			.Add(PKStr, KStr)
			.AddPageQry(PageQry, Lmt, Ofst);
			var RawDicts = SqlCmd.Args(Arg).Run(Ct);
			var PoAsy = RawDicts.Select(R=>T.DbDictToEntity<PoCfg>(R));
			var R = PageAsy.Mk(PageQry, PoAsy);
			return R;
		};
		return Fn;
	}

	public async Task<Func<
		IUserCtx
		,str
		,str
		,CT
		,Task<nil>
	>> FnSetVStrByKStr(IDbFnCtx Ctx, CT Ct){
var NVStr = nameof(PoCfg.VStr); var NOwner = nameof(PoCfg.Owner); var NKStr = nameof(PoCfg.KStr);
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
			.Add(POwner, T.UpperToRaw(User.UserId))
			.Add(PKStr, KStr).Add(PVStr, VStr);
			await SqlCmd.Args(Args).Run(Ct).FirstOrDefaultAsync(Ct);
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
	>> FnSetVI64ByKStr(IDbFnCtx Ctx, CT Ct){
var NVI64 = nameof(PoCfg.VI64); var NOwner = nameof(PoCfg.Owner); var NKStr = nameof(PoCfg.KStr);
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
			.Add(POwner, T.UpperToRaw(User.UserId))
			.Add(PKStr, KStr).Add(PI64, VI64);
			await SqlCmd.Args(Args).Run(Ct).FirstOrDefaultAsync(Ct);
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
