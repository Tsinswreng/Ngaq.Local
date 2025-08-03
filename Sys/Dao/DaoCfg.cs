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

public partial class DaoCfg(
	ISqlCmdMkr SqlCmdMkr
	,ITblMgr TblMgr
	,IAppRepo<PoCfg, IdCfg> RepoCfg
){

	protected ITable T{
		get{return TblMgr.GetTable<PoCfg>();}
	}

	public async Task<Func<
		IUserCtx
		,str
		,IPageQuery
		,CT
		,Task<IPageAsy<PoCfg>>
	>> FnPageByKStr(IDbFnCtx Ctx, CT Ct){
var NOwner = nameof(PoCfg.Owner); var NKStr = nameof(PoCfg.KStr);
var POwner = T.Prm(NOwner);var PKStr = T.Prm(NKStr);
var Sql = $"""
SELECT * FROM {T.Qt(T.DbTblName)}
WHERE {T.Qt(NOwner)} = {POwner}
AND {T.Qt(NKStr)} = {PKStr}
{T.SqlMkr.PrmLmtOfst(out var Lmt, out var Ofst)}
""";
var SqlCmd = await SqlCmdMkr.Prepare(Ctx, Sql, Ct);
		var Fn = async(IUserCtx User, str KStr, IPageQuery PageQry, CT Ct)=>{
			var Arg = ArgDict.Mk()
			.Add(POwner, T.UpperToRaw(User.UserId))
			.Add(PKStr, KStr)
			.AddPageQry(PageQry, Lmt, Ofst);
			var RawDicts = SqlCmd.Args(Arg).Run(Ct);
			var PoAsy = RawDicts.Select(R=>T.DbDictToPo<PoCfg>(R));
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
		var Fn = async(IUserCtx User, str KStr, str VStr, CT Ct)=>{
			var Args = ArgDict.Mk()
			.Add(POwner, T.UpperToRaw(User.UserId))
			.Add(PKStr, KStr).Add(PVStr, VStr);
			await SqlCmd.Args(Args).Run(Ct).FirstOrDefaultAsync(Ct);
			return NIL;
		};
		return Fn;
	}

	// public async Task<Func<
	// 	IUserCtx
	// 	,PoCfg
	// 	,Task<nil>
	// >> FnUdp(IDbFnCtx Ctx, CT Ct){
	// 	var Keys = T.CodeCol_UpperType.Keys;
	// 	var Update = await RepoCfg.FnUpdManyById(Ctx, Keys, Ct);
	// }

}
