#if false
using System.Linq.Expressions;
using Ngaq.Core.Shared.Word.Models.Po.Word;
using Tsinswreng.CsSqlHelper;

namespace Ngaq.Local.Domains.Word.Dao.SqlMkr;


public class QryMkr<T>{
	public QryMkr<T> Select(Expression<Func<T,obj>> Expr){

	}
	public QryMkr<T> Where(Expression<Func<T,bool>> Expr){

	}

	public QryMkr<T> Where(Expression<Func<T, obj>> Expr){

	}

}

public class SqlMkr_2026_0108_170224{
	ITblMgr TblMgr;

	public void Mk2026_0108_173216(){
/*
SELECT {N.Id} FROM {T.DbTblName}
WHERE 1=1
AND {T.Fld(N.Head)} LIKE {PPrefix} || '%'
AND {T.Eq(POwner)}
ORDER BY {T.Fld(N.Head)} ASC
{T.SqlMkr.ParamLimOfst(out var PLmt, out var POfst)}
 */

	}

	public void Mk(){
/*
SELECT {N.Id} FROM {T.DbTblName}
WHERE 1=1
AND {T.Fld(N.Head)} LIKE {PPrefix} || '%'
AND {T.Eq(POwner)}
ORDER BY {T.Fld(N.Head)} ASC
{T.SqlMkr.ParamLimOfst(out var PLmt, out var POfst)}
 */
		var T = TblMgr.GetTbl<PoWord>();
		var qm = new QryMkr<PoWord>();
		qm.Select(x=>x.Id)
		.Where(x=>x.Head)
		;

	}
}

#endif
