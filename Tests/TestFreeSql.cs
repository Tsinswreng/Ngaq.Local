#if false
using System.Threading.Tasks;
using Ngaq.Core.Infra;
using Ngaq.Core.Model;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Tools;
using Ngaq.Local.Db;

namespace Ngaq.Local.Tests;

public  partial class TestFreeSql{
	public async Task TestRepo(){
		var repo = new RepoFsql<Po_Kv, Id_Kv>();
		var Id = new Id_Kv(IdTool.NewUlid_UInt128());
		var po_kv = new Po_Kv{
			Id = Id
			,KStr = "Key1"
			,VStr = "Value1"
		};
		await repo.AddManyAsy([po_kv]);
		var Fsql = FreeSqlCfg.Inst.Fsql;
		var po_kvList = Fsql.Select<Po_Kv>().Where(e=>e.KStr=="Key1").ToList();
		if(po_kvList.Count!=1){
			throw new Exception("po_kvList.Count!=1");
		}
		var first = po_kvList[0];
		if(first.VStr!="Value1"){
			throw new Exception("po_kvList[0].VStr!=\"Value1\"");
		}
		var dict = DictCtx.ToDict(first);
		dict[nameof(po_kv.VDescr)] = "Description1";
		var id_dict = new Id_Dict<Id_Kv>(Id, dict);
		await repo.UpdateManyAsy([id_dict]);
	}
}

#endif
