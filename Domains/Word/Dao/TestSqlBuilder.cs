#if false
using System.Linq.Expressions;

namespace Ngaq.Backend.Domains.Word.Dao;
// 表元数据（强类型，AOT友好）
public interface ITableMeta<T> where T : class {
	string TableName { get; }
	string GetFieldName<TField>(Expression<Func<T, TField>> fieldExpr);
	string GetParamName(string paramKey);
}

// 核心查询构建器（值类型，AOT兼容）
public interface QueryBuilder<T> where T : class {
	// 静态创建
	public static QueryBuilder<T> Create(ITableMeta<T> tableMeta);

	// Select：强类型字段/全字段
	public QueryBuilder<T> Select<TField>(Expression<Func<T, TField>> fieldExpr, string? alias = null);
	public QueryBuilder<T> SelectAll();

	// Where：表达式树条件（自动拼接AND/OR）
	public QueryBuilder<T> Where(Expression<Func<T, bool>> condition);
	public QueryBuilder<T> AndWhere(Expression<Func<T, bool>> condition);

	// 排序
	public QueryBuilder<T> OrderBy<TField>(Expression<Func<T, TField>> fieldExpr, bool isDesc = true);

	// 分页：自动生成Limit/Offset参数
	public QueryBuilder<T> Pagination(out string lmtParam, out string ofstParam);

	// 过滤删除状态（通用扩展）
	public QueryBuilder<T> FilterDeleted(bool includeDeleted);

	// CTE：复杂查询支持
	public QueryBuilder<T> WithCTE(string cteName, QueryBuilder<object> cteQuery, bool isRecursive = false);

	// 构建SQL命令（AOT兼容的预编译命令）
	//public Task<PreparedSqlCmd> BuildAsync(IDbFnCtx ctx, CancellationToken ct);
}

// 分页结果构建（通用扩展）
// public static class PageResultExtensions {
// 	public static async Task<IPageAsyE<T>> BuildPageAsync<T>(
// 		this PreparedSqlCmd cmd,
// 		IPageQry pageQry,
// 		IReadOnlyDictionary<string, object> paramDict,
// 		CancellationToken ct,
// 		Func<IStr_Any, T> mapper,
// 		Func<CancellationToken, Task<ulong>>? countFunc = null);
// }

#endif

