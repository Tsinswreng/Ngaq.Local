using System.CodeDom;
using Ngaq.Core.Infra;
using Ngaq.Core.Infra.Db;
using Ngaq.Core.Model;
using Ngaq.Core.Model.Po;
using Tsinswreng.CsSqlHelper;

namespace Ngaq.Local.Db;

public class RepoFsql
<
	T_Entity
	, T_Id
>
	//: I_Crud<T_Entity>
	where T_Entity: class, I_Id<T_Id>
	where T_Id : IEquatable<T_Id>
{

	public IFreeSql Fsql{get;set;}
	public RepoFsql(){
		Fsql = FreeSqlCfg.Inst.Fsql;
	}

	public async Task<nil> AddManyAsy(
		IEnumerable<T_Entity> EntityList
		,CancellationToken ct = default
	) {
		await Fsql.Insert(EntityList).ExecuteAffrowsAsync(ct); // 批量插入并返回影响行数
		return NIL;
	}


	public async Task<T_Entity?> SeekByIdAsy<T_Id1>(
		T_Id1 Id
		,CancellationToken ct = default
	) {
		var entity = await Fsql.Select<T_Entity>().WhereDynamic(Id).ToOneAsync(ct);
		return entity;
	}

	public async Task<nil> UpdateManyAsy<T_Id1>(
		IEnumerable<Id_Dict<T_Id1>> Id_Dicts
		,CancellationToken ct = default
	) {
		foreach (var id_dict in Id_Dicts) {
			await Fsql.Update<T_Entity>()
				.SetDto(id_dict.Dict) // 动态设置字段
				.WhereDynamic(id_dict.Id)
				.ExecuteAffrowsAsync(ct)
			;
		}
		return NIL;
	}

	public async Task<nil> DeleteManyByIdAsy(
		IEnumerable<object> IdList
		,CancellationToken ct = default
	) {
		await Fsql.Delete<T_Entity>().WhereDynamic(IdList).ExecuteAffrowsAsync(ct);
		return NIL;
	}

}
