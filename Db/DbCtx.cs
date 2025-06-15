#if false
namespace Ngaq.Local.Db;

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Ngaq.Core.Infra;
using Ngaq.Core.Model.Po;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Model.Sys.Po.User;
using Ngaq.Core.Models.Po;
using Tsinswreng.CsUlid;
using ToolId = Tsinswreng.CsUlid.IdTool;


/*
// dotnet ef migrations add Init --project ./Ngaq.Local
// dotnet ef database update --project ./Ngaq.Local

// #dotnet ef dbcontext optimize --output-dir GeneratedInterceptors # --namespace YourProject.GeneratedInterceptors
// dotnet ef dbcontext optimize --output-dir GeneratedInterceptors --precompile-queries --nativeaot

 */

public class LocalDbCtx : DbContext{

	public DbSet<PoWord> PoWord{get;set;}
	public DbSet<PoWordProp> PoKv{get;set;}
	public DbSet<PoWordLearn> PoLearn{get;set;}
	public str DbPath{get;set;} = AppCfg.Inst.SqlitePath;

	protected override void OnConfiguring(DbContextOptionsBuilder opt) {
		base.OnConfiguring(opt);
		// var dbPath = Path.Combine(
		// 	Directory.GetCurrentDirectory(),
		// 	"..", "Ngaq.sqlite"
		// );
		//var dbPath = DbPath;
		var dbPath = "E:/_code/CsNgaq/Ngaq.Sqlite";
		opt.UseSqlite($"Data Source={dbPath}");
	}

	protected nil _CfgPoBase<
		[DynamicallyAccessedMembers(
			DynamicallyAccessedMemberTypes.PublicConstructors |
			DynamicallyAccessedMemberTypes.NonPublicConstructors |
			DynamicallyAccessedMemberTypes.PublicFields |
			DynamicallyAccessedMemberTypes.NonPublicFields |
			DynamicallyAccessedMemberTypes.PublicProperties |
			DynamicallyAccessedMemberTypes.NonPublicProperties |
			DynamicallyAccessedMemberTypes.Interfaces
		)]
		T
	>(ModelBuilder mb) where T:class, IPoBase{
		mb.Entity<T>(e=>{

			e.Property(p=>p.CreatedAt).HasConversion(
				tempus=>tempus.Value,
				val => new Tempus(val)
			);
			e.Property(p=>p.DbCreatedAt).HasConversion(
				tempus=>tempus.Value,
				val => new Tempus(val)
			);

			e.Property(p=>p.UpdatedAt).HasConversion<long?>(
				tempus=>tempus==null?null:tempus.Value.Value,
				val => val==null?null:new Tempus(val.Value)
			);

			e.Property(p=>p.DbUpdatedAt).HasConversion<long?>(
				tempus=>tempus==null?null:tempus.Value.Value,
				val => val==null?null:new Tempus(val.Value)
			);

			e.Property(p=>p.Status).HasConversion(
				status=>status.Value,
				val => new PoStatus(val)
			);


			e.Property(p=>p.CreatedBy).HasConversion(
				id=>id==null?null:id.Value.Value.ToByteArr()
				,val => val==null?null:new IdUser(ToolId.ByteArrToUInt128(val))
			);
			e.HasIndex(p=>p.CreatedBy);

			e.Property(p=>p.LastUpdatedBy).HasConversion(
				id=>id==null?null:id.Value.Value.ToByteArr()
				,val => val==null?null:new IdUser(ToolId.ByteArrToUInt128(val))
			);
			e.HasIndex(p=>p.LastUpdatedBy);

		});
		return NIL;
	}

	protected nil _CfgI_WordId<
		[DynamicallyAccessedMembers(
			DynamicallyAccessedMemberTypes.PublicConstructors |
			DynamicallyAccessedMemberTypes.NonPublicConstructors |
			DynamicallyAccessedMemberTypes.PublicFields |
			DynamicallyAccessedMemberTypes.NonPublicFields |
			DynamicallyAccessedMemberTypes.PublicProperties |
			DynamicallyAccessedMemberTypes.NonPublicProperties |
			DynamicallyAccessedMemberTypes.Interfaces
		)]
		T
	>(ModelBuilder mb) where T:class, I_WordId{
		mb.Entity<T>(e=>{
			e.HasIndex(p=> p.WordId);
			e.Property(p => p.WordId).HasConversion(
				id=> id.Value.ToByteArr()
				,val => IdWord.FromByteArr(val)
			).HasColumnType("BLOB");
		});
		return NIL;
	}

	protected override void OnModelCreating(ModelBuilder mb) {
		base.OnModelCreating(mb);
		_CfgPoBase<PoWord>(mb);
		mb.Entity<PoWord>(e=>{
			e.ToTable("Word");
			e.HasKey(p=>p.Id);
			e.Property(p=>p.Id).HasConversion(
				id=>id.Value.ToByteArr()
				,val => new IdWord(ToolId.ByteArrToUInt128(val))
			).HasColumnType("BLOB");
			e.Property(p=>p.Owner).HasConversion(
				id=>id.Value.ToByteArr()
				,val => new IdUser(ToolId.ByteArrToUInt128(val))
			).HasColumnType("BLOB");
			e.HasIndex(p=>p.Head);
			e.HasIndex(p => new {p.Head, p.Lang, p.Owner}).IsUnique();
			//Unique(WordFormId, Lang):
		});

		_CfgPoBase<PoWordProp>(mb);
		_CfgI_WordId<PoWordProp>(mb);
		mb.Entity<PoWordProp>(e=>{
			e.ToTable("Prop").UseTpcMappingStrategy();
			e.HasKey(p=>p.Id);
			e.Property(p=>p.Id).HasConversion(
				id=>id.Value.ToByteArr()
				,val => new IdWordProp(ToolId.ByteArrToUInt128(val))
			).HasColumnType("BLOB");
			//e.Ignore(p=>p.FKeyUInt128);

			// e.HasIndex(p=>p.FKeyUInt128);
			// e.Property(p=>p.FKeyUInt128).HasConversion(
			// 	id=>id==null?null:id.Value.ToByteArr()
			// 	,val => val==null?null:ToolId.ByteArrToUInt128(val)
			// ).HasColumnType("BLOB");
			e.HasIndex(p=>p.KStr);
			e.HasIndex(p=>p.KI64);
		});

		_CfgPoBase<PoWordLearn>(mb);
		_CfgI_WordId<PoWordLearn>(mb);
		mb.Entity<PoWordLearn>((e=>{
			e.ToTable("Learn").UseTpcMappingStrategy();
			//e.HasKey(p=>p.Id);
			e.Property(p => p.Id).HasConversion(
				id=> id.Value.ToByteArr()
				,val => IdLearn.FromByteArr(val)
			).HasColumnType("BLOB");

			e.HasIndex((p=> p.CreatedAt));
		}));
	}
}


#endif
