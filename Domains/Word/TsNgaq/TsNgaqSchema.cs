using Tsinswreng.CsSqlHelper;

namespace Ngaq.Local.TsNgaq;
using E = Ngaq.Local.TsNgaq.TsNgaqEntities;
public class TsNgaqSchema{
	protected ITblMgr Mgr;
	public TsNgaqSchema(ITblMgr Mgr){
		this.Mgr = Mgr;
	}

	protected ITable Mk<T>(str DbTblName){
		return Table.FnMkTbl<T>(TsNgaqDictMapper.Inst)(DbTblName);
	}

	ITable CfgPoBase(ITable o){
		o.CodeIdName = nameof(E.TsNgaqPoBase.id);
		//o.SetCol(o.CodeIdName).AdditionalSqls([""])
		return o;
	}

	public nil Init(){
		{
			var o = Mk<E.textWord>(nameof(E.textWord));
			Mgr.AddTbl(o);
			CfgPoBase(o);
		}
		{
			var o = Mk<E.property>(nameof(E.property));
			Mgr.AddTbl(o);
			CfgPoBase(o);
		}
		{
			var o = Mk<E.learn>(nameof(E.learn));
			Mgr.AddTbl(o);
			CfgPoBase(o);
		}
		return NIL;
	}
}
