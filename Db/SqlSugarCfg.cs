// using Ngaq.Core.Model.Po.Kv;
// using Ngaq.Core.Model.Po.Word;
// using SqlSugar;
// using System;
// using System.Linq;
// using System.Reflection;

// public class SqlSugarConfig {
// 	public static SqlSugarClient GetDb() {
// 		var dbPath = Path.Combine(
// 			Directory.GetCurrentDirectory(),
// 			"..", "Ngaq.sqlite"
// 		);

// 		var db = new SqlSugarClient(new ConnectionConfig {
// 			ConnectionString = $"Data Source={dbPath}",
// 			DbType = DbType.Sqlite,
// 			IsAutoCloseConnection = true
// 		});

// 		// 全局配置类型转换
// 		ConfigureUInt128Converter(db);

// 		// 动态配置表名、主键、列
// 		ConfigureEntities(db);

// 		// 初始化表结构
// 		db.CodeFirst.InitTables(typeof(Po_Word), typeof(Po_Kv));

// 		// 手动创建索引
// 		CreateIndexes(db);

// 		return db;
// 	}

// 	private static void ConfigureUInt128Converter(SqlSugarClient db) {
// 		// 全局配置 UInt128 类型的转换
// 		db.CurrentConnectionConfig.ConfigureExternalServices = new ConfigureExternalServices {
// 			EntityService = (prop, column) => {
// 				if (prop.PropertyType == typeof(Id_Word) || prop.PropertyType == typeof(Id_Kv)) {
// 					column.IsPrimarykey = true;
// 					column.DataType = "BLOB";
// 					column.SqlParameterDbType = System.Data.DbType.Binary;
// 				} else if (prop.PropertyType == typeof(UInt128?)) {
// 					column.DataType = "BLOB";
// 					column.SqlParameterDbType = System.Data.DbType.Binary;
// 				}
// 			}
// 		};
// 	}

// 	private static void ConfigureEntities(SqlSugarClient db) {
// 		// 动态配置 Po_Word
// 		db.MappingTables.Add(nameof(Po_Word), "Word");

// 		db.CodeFirst.ConfigEntity<Po_Word>(e => {
// 			e.Property(p => p.Id).HasColumnConverter(
// 				id => id.Value.ToByteArr(),
// 				val => new Id_Word(IdUtil.ByteArrToUInt128(val))
// 			);
// 		});

// 		// 动态配置 Po_Kv
// 		db.MappingTables.Add(nameof(Po_Kv), "Kv");
// 		db.CodeFirst.ConfigEntity<Po_Kv>(e => {
// 			e.Property(p => p.SubjectId_UInt128).HasColumnConverter(
// 				id => id == null ? null : id.Value.ToByteArr(),
// 				val => val == null ? null : IdUtil.ByteArrToUInt128(val)
// 			);
// 		});


// 	// 全局注册UInt128转换器
// 	SqlSugar.Ado.SqlServerCodeFirst.SetStringConvertFunc<Id_Word>(id => id.Value.ToByteArr());
// 	SqlSugar.Ado.SqlServerCodeFirst.SetStringConvertFunc<Id_Kv>(id => id.Value.ToByteArr());


// 	}

// 	private static void CreateIndexes(SqlSugarClient db) {
// 		// 创建 Po_Word 表的索引
// 		if (!db.DbMaintenance.IsAnyIndex("Word", "idx_WordFormId")) {
// 			db.DbMaintenance.CreateIndex("Word", new[] { "WordFormId" }, "idx_WordFormId");
// 		}
// 		if (!db.DbMaintenance.IsAnyIndex("Word", "idx_WordFormId_Lang")) {
// 			db.DbMaintenance.CreateIndex("Word", new[] { "WordFormId", "Lang" }, "idx_WordFormId_Lang", true);
// 		}

// 		// 创建 Po_Kv 表的索引
// 		if (!db.DbMaintenance.IsAnyIndex("Kv", "idx_SubjectId")) {
// 			db.DbMaintenance.CreateIndex("Kv", new[] { "SubjectId_UInt128" }, "idx_SubjectId");
// 		}
// 		if (!db.DbMaintenance.IsAnyIndex("Kv", "idx_KStr")) {
// 			db.DbMaintenance.CreateIndex("Kv", new[] { "KStr" }, "idx_KStr");
// 		}
// 		if (!db.DbMaintenance.IsAnyIndex("Kv", "idx_KI64")) {
// 			db.DbMaintenance.CreateIndex("Kv", new[] { "KI64" }, "idx_KI64");
// 		}
// 	}
// }
