namespace Ngaq.Local.TsNgaq;
using System.Data;
using Microsoft.Data.Sqlite;
using Ngaq.Local.Db.TswG;
using Tsinswreng.CsDictMapper;
using Tsinswreng.CsSqlHelper;
using Tsinswreng.CsSqlHelper.Sqlite;
using E = Ngaq.Local.TsNgaq.TsNgaqEntities;

public class TsNgaqDependency{
	public str DbPath{get;set;}
	public TsNgaqDependency(str DbPath){
		this.DbPath = DbPath;

		DbConn = new SqliteConnection($"Data Source={DbPath}");
		DbConn.Open();

		DictMapper = TsNgaqDictMapper.Inst;

		TblMgr = new TsNgaqTblMgr();
		TsNgaqSchema = new TsNgaqSchema(TblMgr);
		TsNgaqSchema.Init();

		SqlCmdMkr = new SqliteCmdMkr(DbConn);
		this.RepoTextWord = new AppRepo<E.textWord, i64>(TblMgr, SqlCmdMkr, DictMapper);
		this.RepoProp = new AppRepo<E.property, i64>(TblMgr, SqlCmdMkr, DictMapper);
		this.RepoLearn = new AppRepo<E.learn, i64>(TblMgr, SqlCmdMkr, DictMapper);
		TsNgaqSvc = new TsNgaqSvc(SqlCmdMkr, TblMgr, RepoTextWord, RepoProp, RepoLearn);
	}


	public IDictMapperShallow DictMapper{get;set;}
	public IDbConnection DbConn{get;set;}
	public ISqlCmdMkr SqlCmdMkr{get;set;}
	public ITblMgr TblMgr{get;set;}
	public TsNgaqSchema TsNgaqSchema{get;set;}
	public IAppRepo<E.textWord, i64> RepoTextWord{get;set;}
	public IAppRepo<E.property, i64> RepoProp{get;set;}
	public IAppRepo<E.learn, i64> RepoLearn{get;set;}

	public TsNgaqSvc TsNgaqSvc{get;set;}
}
