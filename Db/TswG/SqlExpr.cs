using System.Linq.Expressions;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Models.Po;
using Tsinswreng.CsSqlHelper;


namespace Ngaq.Local.Db.TswG;

public partial class SqlExpr<T>{

	public SqlExpr<T> Select(
		Expression<Func<T, object>> expr
	){
		return this;
	}

	public SqlExpr<T> Where(Expression<Func<T, bool>> expr){
		return this;
	}

	public SqlExpr<T> OrderBy(Expression<Func<T, obj>> Expr){
		return this;
	}

	public SqlExpr<T> Page(u64 PageNum, u64 PageSize){
		return this;
	}

	public str ToSql(){
		return "";
	}
}

public static class ExtnSqlExpr{
	public static object? Prm<TEntity>(
		this SqlExpr<TEntity> z,
		str Name
	){
		var TblMgr = (ITblMgr)LocalTblMgr.Inst;
		var Tbl = TblMgr.GetTbl<TEntity>();
		return Tbl.Prm(Name);
	}
	public static object? Prm<TEntity>(
		this SqlExpr<TEntity> z
	){
		return "";
	}
	public static obj Star<TEntity>(
		this SqlExpr<TEntity> z
	){
		return "*";
	}
}


class Test_(
	ISqlCmdMkr SqlCmdMkr
	,ITblMgr TblMgr
){
	void Test(){
		var TW = TblMgr.GetTbl<PoWord>();
		var NOwner = nameof(PoWord.Owner);
		var E = new SqlExpr<PoWord>();
		E
			.Select(w=>E.Star())
			.Where(w=>w.Owner.Equals(E.Prm(NOwner))
				&& w.Status != PoStatus.Deleted.Value
			)
			.OrderBy(x=>x.CreatedAt)
			.Page(2,16)
		;
	}
}
