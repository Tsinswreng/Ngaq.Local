using Microsoft.Extensions.DependencyInjection;
using Ngaq.Core.Infra;
using Tsinswreng.CsSql;
using Tsinswreng.CsTempus;

namespace Ngaq.Backend.Db.TswG.Migrations;
using Self = M2026_0428_225820;

/// Local 端新增 StudyPlan 結構的增量遷移。
///
/// 它只關心「StudyPlan 這次新增了什麼」，
/// 不重新聲明初始版本的其它表。
public class M2026_0428_225820:SqlMigrationInfo{
	/// 本次 StudyPlan 遷移的版本號。
	/// 注意：此遷移在未發版前擴充了「舊枚舉整數轉 TEXT」的數據修正，
	/// 因此必須更新 CreatedMs，確保已跑過舊草稿版本的本地庫會重新執行。
	public override i64 CreatedMs{get;set;} = UnixMs.FromIso("2026-04-28T22:58:20.023+08:00");

	IServiceProvider Sp;
	IMkrTblMgr MkrTblMgr;
	public M2026_0428_225820(
		IServiceProvider Sp
	){
		this.Sp = Sp;
		this.MkrTblMgr = Sp.GetRequiredService<IMkrTblMgr>();
		Init();
	}
	/// 只註冊 StudyPlan 相關結構，再生成本次遷移所需 DDL。
	void Init(){
		var Mgr = MkrTblMgr.MkTblMgr();
		LocalTblMgrIniter.InitStudyPlan(Mgr);
		LocalTblMgrIniter.InitNormLang(Mgr);
		LocalTblMgrIniter.InitUserLang(Mgr);
		LocalTblMgrIniter.InitNormLangToUserLang(Mgr);
		SqlsUp = [
			..Mgr.SqlsMkSchema(),
			// 舊版 sqlite 中，Kv / WordProp / WordLearn 的枚舉列以整數存儲。
			// 這裏不能只改值，還要把表結構中的列定義一併改成當前 TEXT 規範。
			// sqlite 不支持直接改列類型，因此採用「重建表 + 搬數據 + 重建索引」方案。
			"""
ALTER TABLE "Kv" RENAME TO "__Mig20260428_Kv_Old"
""",
			"""
CREATE TABLE "Kv"(
	"Id" BLOB PRIMARY KEY,
	"Owner" BLOB,
	"KType" TEXT,
	"KStr" TEXT,
	"KI64" INTEGER,
	"VType" TEXT,
	"VStr" TEXT,
	"VI64" INTEGER,
	"VF64" REAL,
	"VBinary" BLOB,
	"DbCreatedAt" INTEGER,
	"DbUpdatedAt" INTEGER,
	"DelAt" INTEGER
)
""",
			"""
INSERT INTO "Kv"(
	"Id","Owner","KType","KStr","KI64","VType","VStr","VI64","VF64","VBinary","DbCreatedAt","DbUpdatedAt","DelAt"
)
SELECT
	"Id",
	"Owner",
	CASE
		WHEN typeof("KType") = 'integer' THEN CASE CAST("KType" AS INTEGER)
			WHEN 1 THEN 'Str'
			WHEN 2 THEN 'I64'
			WHEN 3 THEN 'F64'
			WHEN 4 THEN 'Binary'
			ELSE CAST("KType" AS TEXT)
		END
		ELSE "KType"
	END,
	"KStr",
	"KI64",
	CASE
		WHEN typeof("VType") = 'integer' THEN CASE CAST("VType" AS INTEGER)
			WHEN 1 THEN 'Str'
			WHEN 2 THEN 'I64'
			WHEN 3 THEN 'F64'
			WHEN 4 THEN 'Binary'
			ELSE CAST("VType" AS TEXT)
		END
		ELSE "VType"
	END,
	"VStr",
	"VI64",
	"VF64",
	"VBinary",
	"DbCreatedAt",
	"DbUpdatedAt",
	"DelAt"
FROM "__Mig20260428_Kv_Old"
""",
			"""
DROP TABLE "__Mig20260428_Kv_Old"
""",
			"""
CREATE INDEX "Idx_Kv_Owner" ON "Kv"("Owner")
""",
			"""
CREATE INDEX "Idx_Kv_KStr" ON "Kv"("KStr")
""",
			"""
CREATE INDEX "Idx_Kv_KI64" ON "Kv"("KI64")
""",
			"""
CREATE UNIQUE INDEX "Ux_Kv_Owner_KStr_Str"
ON "Kv"("Owner", "KStr")
WHERE ("DelAt" = 0) AND "KType" = 'Str'
""",
			"""
CREATE UNIQUE INDEX "Ux_Kv_Owner_KI64_I64"
ON "Kv"("Owner", "KI64")
WHERE ("DelAt" = 0) AND "KType" = 'I64'
""",
			"""
ALTER TABLE "WordProp" RENAME TO "__Mig20260428_WordProp_Old"
""",
			"""
CREATE TABLE "WordProp"(
	"Id" BLOB PRIMARY KEY,
	"WordId" BLOB,
	"KType" TEXT,
	"KStr" TEXT,
	"KI64" INTEGER,
	"VType" TEXT,
	"VStr" TEXT,
	"VI64" INTEGER,
	"VF64" REAL,
	"VBinary" BLOB,
	"DbCreatedAt" INTEGER,
	"DbUpdatedAt" INTEGER,
	"DelAt" INTEGER,
	"BizCreatedAt" INTEGER,
	"BizUpdatedAt" INTEGER
)
""",
			"""
INSERT INTO "WordProp"(
	"Id","WordId","KType","KStr","KI64","VType","VStr","VI64","VF64","VBinary","DbCreatedAt","DbUpdatedAt","DelAt","BizCreatedAt","BizUpdatedAt"
)
SELECT
	"Id",
	"WordId",
	CASE
		WHEN typeof("KType") = 'integer' THEN CASE CAST("KType" AS INTEGER)
			WHEN 1 THEN 'Str'
			WHEN 2 THEN 'I64'
			WHEN 3 THEN 'F64'
			WHEN 4 THEN 'Binary'
			ELSE CAST("KType" AS TEXT)
		END
		ELSE "KType"
	END,
	"KStr",
	"KI64",
	CASE
		WHEN typeof("VType") = 'integer' THEN CASE CAST("VType" AS INTEGER)
			WHEN 1 THEN 'Str'
			WHEN 2 THEN 'I64'
			WHEN 3 THEN 'F64'
			WHEN 4 THEN 'Binary'
			ELSE CAST("VType" AS TEXT)
		END
		ELSE "VType"
	END,
	"VStr",
	"VI64",
	"VF64",
	"VBinary",
	"DbCreatedAt",
	"DbUpdatedAt",
	"DelAt",
	"BizCreatedAt",
	"BizUpdatedAt"
FROM "__Mig20260428_WordProp_Old"
""",
			"""
DROP TABLE "__Mig20260428_WordProp_Old"
""",
			"""
CREATE INDEX "Idx_WordProp_WordId" ON "WordProp"("WordId")
""",
			"""
CREATE INDEX "Idx_WordProp_KStr" ON "WordProp"("KStr")
""",
			"""
CREATE INDEX "Idx_WordProp_KI64" ON "WordProp"("KI64")
""",
			"""
ALTER TABLE "WordLearn" RENAME TO "__Mig20260428_WordLearn_Old"
""",
			"""
CREATE TABLE "WordLearn"(
	"Id" BLOB PRIMARY KEY,
	"WordId" BLOB,
	"LearnResult" TEXT,
	"DbCreatedAt" INTEGER,
	"DbUpdatedAt" INTEGER,
	"DelAt" INTEGER,
	"BizCreatedAt" INTEGER,
	"BizUpdatedAt" INTEGER
)
""",
			"""
INSERT INTO "WordLearn"(
	"Id","WordId","LearnResult","DbCreatedAt","DbUpdatedAt","DelAt","BizCreatedAt","BizUpdatedAt"
)
SELECT
	"Id",
	"WordId",
	CASE
		WHEN typeof("LearnResult") = 'integer' THEN CASE CAST("LearnResult" AS INTEGER)
			WHEN 1 THEN 'Add'
			WHEN 2 THEN 'Rmb'
			WHEN 3 THEN 'Fgt'
			ELSE CAST("LearnResult" AS TEXT)
		END
		ELSE "LearnResult"
	END,
	"DbCreatedAt",
	"DbUpdatedAt",
	"DelAt",
	"BizCreatedAt",
	"BizUpdatedAt"
FROM "__Mig20260428_WordLearn_Old"
""",
			"""
DROP TABLE "__Mig20260428_WordLearn_Old"
""",
			"""
CREATE INDEX "Idx_WordLearn_WordId" ON "WordLearn"("WordId")
"""
		];
	}
}
