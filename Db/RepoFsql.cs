using System.CodeDom;
using Ngaq.Core.Infra;
using Ngaq.Core.Model;
using Ngaq.Core.Model.Po;

namespace Ngaq.Local.Db;

public class RepoFsql
<
	T_Entity
	, T_Id
>
	: I_Crud<T_Entity>
	where T_Entity: class, I_Id<T_Id>
	where T_Id : IEquatable<T_Id>
{

	public IFreeSql Fsql{get;set;}
	public RepoFsql(){
		Fsql = FreeSqlCfg.Inst.Fsql;
	}

	public async Task<I_Answer<nil>> AddManyAsy(IEnumerable<T_Entity> EntityList) {
		I_Answer<nil> ans = new Answer<nil>();
		using(var uow = Fsql.CreateUnitOfWork()){//創建事務單元
			try{
				await Fsql.Insert(EntityList).ExecuteAffrowsAsync(); // 批量插入并返回影响行数
				uow.Commit();
				return ans.OkWith(Nil);
			}
			catch (System.Exception e){
				uow.Rollback();
				ans.AddErrException(e);
				//throw;
			}
			return ans;
		}


	}


	public async Task<I_Answer<T_Entity?>> SeekByIdAsy<T_Id1>(T_Id1 Id) {
		I_Answer<T_Entity?> ans = new Answer<T_Entity?>();
		var entity = await Fsql.Select<T_Entity>().WhereDynamic(Id).ToOneAsync();
		return ans.OkWith(entity);
	}

	public async Task<I_Answer<nil>> UpdateManyAsy<T_Id1>(IEnumerable<Id_Dict<T_Id1>> Id_Dicts) {
		I_Answer<nil> ans = new Answer<nil>();
		using var uow = Fsql.CreateUnitOfWork();
		try{
			foreach (var id_dict in Id_Dicts) {
				await Fsql.Update<T_Entity>()
					.SetDto(id_dict.Dict) // 动态设置字段
					.WhereDynamic(id_dict.Id)
					.ExecuteAffrowsAsync()
				;
			}
			uow.Commit();
			return ans.OkWith(Nil);
		}
		catch (System.Exception e){
			uow.Rollback();
			ans.AddErrException(e);
			//throw;
		}
		return ans;
	}

	public async Task<I_Answer<nil>> DeleteManyByIdAsy(IEnumerable<object> IdList) {
		I_Answer<nil> ans = new Answer<nil>();
		using var uow = Fsql.CreateUnitOfWork();
		try{
			await Fsql.Delete<T_Entity>().WhereDynamic(IdList).ExecuteAffrowsAsync();
			uow.Commit();
			return ans.OkWith(Nil);
		}
		catch (System.Exception e){
			uow.Rollback();
			ans.AddErrException(e);
		}
		return ans;
	}

}
