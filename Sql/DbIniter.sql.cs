namespace Ngaq.Local.Sql;

public partial class DbIniter{

public str Sql{get;set;} =
"""
CREATE TABLE IF NOT EXISTS "__TsinswrengSchemaHistory"(
		"Id" INTEGER PRIMARY KEY,
		"CreatedAt" INTEGER,
		"Name" TEXT,
		"Descr" TEXT,
		"ProductVersionTime" INTEGER
);

CREATE TABLE IF NOT EXISTS "Word"(
		"Id" BLOB PRIMARY KEY,
		"Owner" BLOB,
		"Head" TEXT,
		"Lang" TEXT,
		"CreatedAt" INTEGER,
		"DbCreatedAt" INTEGER,
		"UpdatedAt" INTEGER,
		"DbUpdatedAt" INTEGER,
		"CreatedBy" BLOB,
		"LastUpdatedBy" BLOB,
		"Status" INTEGER,

UNIQUE("Owner", "Head", "Lang")
);
CREATE INDEX "IdxWordHeadLang" ON "Word"("Head", "Lang");

CREATE TABLE IF NOT EXISTS "WordProp"(
		"Id" BLOB PRIMARY KEY,
		"WordId" BLOB,
		"KType" INTEGER,
		"KStr" TEXT,
		"KI64" INTEGER,
		"KDescr" TEXT,
		"VType" INTEGER,
		"VDescr" TEXT,
		"VStr" TEXT,
		"VI64" INTEGER,
		"VF64" REAL,
		"CreatedAt" INTEGER,
		"DbCreatedAt" INTEGER,
		"UpdatedAt" INTEGER,
		"DbUpdatedAt" INTEGER,
		"CreatedBy" BLOB,
		"LastUpdatedBy" BLOB,
		"Status" INTEGER
);
CREATE INDEX "IdxKStr" ON "WordProp" ("KStr");
CREATE INDEX "IdxKI64" ON "WordProp" ("KI64");

CREATE TABLE IF NOT EXISTS "WordLearn"(
		"Id" BLOB PRIMARY KEY,
		"WordId" BLOB,
		"LearnResult" TEXT,
		"CreatedAt" INTEGER,
		"DbCreatedAt" INTEGER,
		"UpdatedAt" INTEGER,
		"DbUpdatedAt" INTEGER,
		"CreatedBy" BLOB,
		"LastUpdatedBy" BLOB,
		"Status" INTEGER
);

""";

}

