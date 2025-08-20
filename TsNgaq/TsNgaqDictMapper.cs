namespace Ngaq.Local.TsNgaq;
using Tsinswreng.CsDictMapper;
using E = Ngaq.Local.TsNgaq.TsNgaqEntities;


[DictType(typeof(E.TsNgaqPoBase))]
[DictType(typeof(E.textWord))]
[DictType(typeof(E.property))]
[DictType(typeof(E.learn))]
public partial class TsNgaqDictMapper{
	protected static TsNgaqDictMapper? _Inst = null;
	public static TsNgaqDictMapper Inst => _Inst??= new TsNgaqDictMapper();
}
