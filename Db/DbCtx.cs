namespace Ngaq.Local.Db;

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Ngaq.Core.Model.Po;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Model.Sys.Po.User;
using Ngaq.Core.Tools;


/*
dotnet ef migrations add Init --project ./Ngaq.Local
dotnet ef database update --project ./Ngaq.Local

#dotnet ef dbcontext optimize --output-dir GeneratedInterceptors # --namespace YourProject.GeneratedInterceptors
dotnet ef dbcontext optimize --output-dir GeneratedInterceptors --precompile-queries --nativeaot

 */

public class LocalDbCtx : DbContext{

	public DbSet<PoWord> Po_Word{get;set;}
	public DbSet<PoKv> Po_Kv{get;set;}
	public DbSet<PoLearn> Po_Learn{get;set;}

	protected override void OnConfiguring(DbContextOptionsBuilder opt) {
		base.OnConfiguring(opt);
		// var dbPath = Path.Combine(
		// 	Directory.GetCurrentDirectory(),
		// 	"..", "Ngaq.sqlite"
		// );
		var dbPath = "E:/_code/CsNgaq/Ngaq.Sqlite";//TODO
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
			e.Property(p=>p.CreatedBy).HasConversion(
				id=>id==null?null:id.Value.Value.ToByteArr()
				,val => val==null?null:new IdUser(ToolId.ByteArrToUInt128(val))
			);
			e.HasIndex(p=>p.CreatedBy);

			e.Property(p=>p.LastUpdatedBy).HasConversion(
				id=>id==null?null:id.Value.Value.ToByteArr()
				,val => val==null?null:new IdUser(ToolId.ByteArrToUInt128(val))
			);
			e.HasIndex(p=>p.CreatedBy);

		});
		return Nil;
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
			e.HasIndex(p=>p.WordFormId);
			e.HasIndex(p => new {p.WordFormId, p.Lang, p.Owner}).IsUnique();
			//Unique(WordFormId, Lang):
		});

		_CfgPoBase<PoKv>(mb);
		mb.Entity<PoKv>(e=>{
			e.ToTable("Prop").UseTpcMappingStrategy();
			e.HasKey(p=>p.Id);
			e.Property(p=>p.Id).HasConversion(
				id=>id.Value.ToByteArr()
				,val => new IdKv(ToolId.ByteArrToUInt128(val))
			).HasColumnType("BLOB");

			e.HasIndex(p=>p.FKey_UInt128);
			e.Property(p=>p.FKey_UInt128).HasConversion(
				id=>id==null?null:id.Value.ToByteArr()
				,val => val==null?null:ToolId.ByteArrToUInt128(val)
			).HasColumnType("BLOB");
			e.HasIndex(p=>p.KStr);
			e.HasIndex(p=>p.KI64);
		});

		mb.Entity<PoLearn>(e=>{
			e.ToTable("Learn").UseTpcMappingStrategy();
			//e.HasKey(p=>p.Id);
			e.Property(p=>p.Id).HasConversion(
				id=>id.Value.ToByteArr()
				,val => new IdLearn(ToolId.ByteArrToUInt128(val))
			).HasColumnType("BLOB");

			e.HasIndex(p=>p.FKey_UInt128);
			e.Property(p=>p.FKey_UInt128).HasConversion(
				id=>id==null?null:id.Value.ToByteArr()
				,val => val==null?null:ToolId.ByteArrToUInt128(val)
			).HasColumnType("BLOB");
			e.HasIndex(p=>p.KStr);
			e.HasIndex(p=>p.KI64);
		});
	}
}

