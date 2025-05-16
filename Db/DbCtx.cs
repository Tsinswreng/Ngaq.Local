namespace Ngaq.Local.Db;

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Ngaq.Core.Model.Po;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn;
using Ngaq.Core.Model.Po.User;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Tools;

/*
dotnet ef migrations add Init --project ./Ngaq.Local
dotnet ef database update --project ./Ngaq.Local


dotnet ef dbcontext optimize --output-dir GeneratedInterceptors # --namespace YourProject.GeneratedInterceptors

 */

public class DbCtx : DbContext{

	public DbSet<Po_Word> Po_Word{get;set;}
	public DbSet<Po_Kv> Po_Kv{get;set;}
	public DbSet<Po_Learn> Po_Learn{get;set;}

	protected override void OnConfiguring(DbContextOptionsBuilder opt) {
		base.OnConfiguring(opt);
		var dbPath = Path.Combine(
			Directory.GetCurrentDirectory(),
			"..", "Ngaq.sqlite"
		);
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
	>(ModelBuilder mb) where T:class, I_PoBase{
		mb.Entity<T>(e=>{
			e.Property(p=>p.CreatedBy).HasConversion(
				id=>id==null?null:id.Value.Value.ToByteArr()
				,val => val==null?null:new Id_User(IdTool.ByteArrToUInt128(val))
			);
			e.HasIndex(p=>p.CreatedBy);

			e.Property(p=>p.LastUpdatedBy).HasConversion(
				id=>id==null?null:id.Value.Value.ToByteArr()
				,val => val==null?null:new Id_User(IdTool.ByteArrToUInt128(val))
			);
			e.HasIndex(p=>p.CreatedBy);

		});
		return Nil;
	}

	protected override void OnModelCreating(ModelBuilder mb) {
		base.OnModelCreating(mb);
		_CfgPoBase<Po_Word>(mb);
		mb.Entity<Po_Word>(e=>{
			e.ToTable("Word");
			e.HasKey(p=>p.Id);
			e.Property(p=>p.Id).HasConversion(
				id=>id.Value.ToByteArr()
				,val => new Id_Word(IdTool.ByteArrToUInt128(val))
			).HasColumnType("BLOB");
			e.Property(p=>p.Owner).HasConversion(
				id=>id.Value.ToByteArr()
				,val => new Id_User(IdTool.ByteArrToUInt128(val))
			).HasColumnType("BLOB");
			e.HasIndex(p=>p.WordFormId);
			e.HasIndex(p => new {p.WordFormId, p.Lang, p.Owner}).IsUnique();
			//Unique(WordFormId, Lang):
		});

		_CfgPoBase<Po_Kv>(mb);
		mb.Entity<Po_Kv>(e=>{
			e.ToTable("Kv").UseTpcMappingStrategy();
			e.HasKey(p=>p.Id);
			e.Property(p=>p.Id).HasConversion(
				id=>id.Value.ToByteArr()
				,val => new Id_Kv(IdTool.ByteArrToUInt128(val))
			).HasColumnType("BLOB");

			e.HasIndex(p=>p.FKey_UInt128);
			e.Property(p=>p.FKey_UInt128).HasConversion(
				id=>id==null?null:id.Value.ToByteArr()
				,val => val==null?null:IdTool.ByteArrToUInt128(val)
			).HasColumnType("BLOB");
			e.HasIndex(p=>p.KStr);
			e.HasIndex(p=>p.KI64);
		});

		mb.Entity<Po_Learn>(e=>{
			e.ToTable("Learn").UseTpcMappingStrategy();
			//e.HasKey(p=>p.Id);
			e.Property(p=>p.Id).HasConversion(
				id=>id.Value.ToByteArr()
				,val => new Id_Kv(IdTool.ByteArrToUInt128(val))
			).HasColumnType("BLOB");

			e.HasIndex(p=>p.FKey_UInt128);
			e.Property(p=>p.FKey_UInt128).HasConversion(
				id=>id==null?null:id.Value.ToByteArr()
				,val => val==null?null:IdTool.ByteArrToUInt128(val)
			).HasColumnType("BLOB");
			e.HasIndex(p=>p.KStr);
			e.HasIndex(p=>p.KI64);
		});
	}
}
