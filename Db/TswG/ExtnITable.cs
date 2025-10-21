namespace Ngaq.Local.Db.TswG;
using Tsinswreng.CsSqlHelper;

public static class ExtnITable{
	public static str SqlIsNonDel(
		this ITable z
	){
		if(z.SoftDelCol is null){
			throw new InvalidOperationException("Soft delete column is not defined.");
		}
		var FieldExpr = z.Fld(z.SoftDelCol.CodeColName);
		return $"({FieldExpr} = 0)";
	}
}
