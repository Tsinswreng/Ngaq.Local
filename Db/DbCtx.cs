namespace Ngaq.Local.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Util;

/*
dotnet ef migrations add Init --project ./Ngaq.Local
dotnet ef database update --project ./Ngaq.Local
 */

public class DbCtx : DbContext{

	public DbSet<Po_Word> Po_Word{get;set;}
	public DbSet<Po_Kv> Po_Kv{get;set;}

	protected override void OnConfiguring(DbContextOptionsBuilder opt) {
		base.OnConfiguring(opt);
		var dbPath = Path.Combine(
			Directory.GetCurrentDirectory(),
			"..", "Ngaq.sqlite"
		);
		opt.UseSqlite($"Data Source={dbPath}");
	}

	protected override void OnModelCreating(ModelBuilder mb) {
		base.OnModelCreating(mb);
		mb.Entity<Po_Word>(e=>{
			e.ToTable("Word");
			e.HasKey(p=>p.Id);
			e.Property(p=>p.Id).HasConversion(
				id=>id.Value.ToByteArr()
				,val => new Id_Word(IdUtil.ByteArrToUInt128(val))
			).HasColumnType("BLOB");
			e.HasIndex(p=>p.WordFormId);
			e.HasIndex(p => new {p.WordFormId, p.Lang}).IsUnique();
			//Unique(WordFormId, Lang):
		});

		mb.Entity<Po_Kv>(e=>{
			e.ToTable("Kv");
			e.HasKey(p=>p.Id);
			e.Property(p=>p.Id).HasConversion(
				id=>id.Value.ToByteArr()
				,val => new Id_Kv(IdUtil.ByteArrToUInt128(val))
			).HasColumnType("BLOB");

			e.HasIndex(p=>p.SubjectId_UInt128);
			e.Property(p=>p.SubjectId_UInt128).HasConversion(
				id=>id==null?null:id.Value.ToByteArr() //爲甚麼這裏不能寫id.ToByteArr()或id!.ToByteArr()
				,val => val==null?null:IdUtil.ByteArrToUInt128(val)
			).HasColumnType("BLOB");
			e.HasIndex(p=>p.KStr);
			e.HasIndex(p=>p.KI64);
		});
	}
}
