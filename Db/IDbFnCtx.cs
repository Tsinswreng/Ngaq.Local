namespace Ngaq.Local.Db;

public interface IDbFnCtx
	:Tsinswreng.CsSqlHelper.IBaseDbFnCtx
{

}

public class DbFnCtx : Tsinswreng.CsSqlHelper.BaseDbFnCtx, IDbFnCtx{

}
