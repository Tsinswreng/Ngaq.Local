// using FreeSql;
// using Ngaq.Core.Model.Po.Word;

// namespace Ngaq.Local.Db;


// public class FreeSql2 {

// 	public IFreeSql Fsql { get; set; }
// 	FreeSql2() {

// 		// FreeSql 配置（Fluent API 方式）
// 		var fsql = new FreeSqlBuilder()
// 			.UseConnectionString(DataType.Sqlite, "Data Source=../Ngaq.sqlite")
// 			.Build()
// 		;

// 		// Po_Word 实体配置
// 		fsql.CodeFirst
// 			.ConfigEntity<Po_Word>(e => {
// 				e.Name("Word"); // 设置表名 [[1]]
// 				e.Property(p => p.Id)
// 					.DbType("BLOB") // 主键类型转换 [[1]]
// 					.HasConversion(
// 						id => id.Value.ToByteArr(),
// 						val => new Id_Word(IdTool.ByteArrToUInt128(val))
// 					);

// 				// 索引配置
// 				e.Index(p => p.WordFormId).Name("idx_WordFormId");
// 				e.Index(p => new { p.WordFormId, p.Lang }).IsUnique().Name("idx_WordFormId_Lang"); // 组合唯一索引 [[1]]
// 			});

// 		// Po_Kv 实体配置
// 		fsql.CodeFirst
// 			.ConfigEntity<Po_Kv>(e => {
// 				e.Name("Kv"); // 设置表名 [[1]]
// 				e.Property(p => p.Id)
// 					.DbType("BLOB") // 主键类型转换 [[1]]
// 					.HasConversion(
// 						id => id.Value.ToByteArr(),
// 						val => new Id_Kv(IdTool.ByteArrToUInt128(val))
// 					);

// 				// 可空字段转换
// 				e.Property(p => p.SubjectId_UInt128)
// 					.DbType("BLOB")
// 					.HasConversion(
// 						id => id?.ToByteArr(),
// 						val => val == null ? null : IdTool.ByteArrToUInt128(val)
// 					);

// 				// 多个索引
// 				e.Index(p => p.SubjectId_UInt128).Name("idx_SubjectId");
// 				e.Index(p => p.KStr).Name("idx_KStr");
// 				e.Index(p => p.KI64).Name("idx_KI64");
// 			});

// 	}


// }
