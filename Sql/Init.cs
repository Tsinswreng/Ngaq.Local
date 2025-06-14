namespace Ngaq.Local.Sql;

using Ngaq.Core.Model.Po;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Model.Po.Word;
using Ngaq.Core.Models.Po;
using Tsinswreng.CsSqlHelper;

public class SqlLatest{

	List<str> Segs = [];

	void Put(str s){
		Segs.Add(s);
	}

	public str GenSql(){

ITblMgr TblMgr = AppTableMgr.Inst;
var T = TblMgr.GetTable<PoWord>();
str Sql;

str Q(str s){
	return T.Quote(s);
}

#region Base
str Id = Q(nameof(I_Id<nil>.Id));
str CreatedAt = Q(nameof(IPoBase.CreatedAt));
str DbCreatedAt = Q(nameof(IPoBase.DbCreatedAt));
str UpdatedAt = Q(nameof(IPoBase.UpdatedAt));
str DbUpdatedAt = Q(nameof(IPoBase.DbUpdatedAt));
str CreatedBy = Q(nameof(IPoBase.CreatedBy));
str LastUpdatedBy = Q(nameof(IPoBase.LastUpdatedBy));
str Status = Q(nameof(IPoBase.Status));
#endregion Base

#region I_WordId
str WordId = Q(nameof(I_WordId.WordId));
#endregion I_WordId

#region PoWord
str Owner = Q(nameof(PoWord.Owner));
str Head = Q(nameof(PoWord.Head));
str Lang = Q(nameof(PoWord.Lang));
#endregion PoWord


#region PoKv
str KType = Q(nameof(PoWordProp.KType));
str KStr = Q(nameof(PoWordProp.KStr));
str KI64 = Q(nameof(PoWordProp.KI64));
str KDescr = Q(nameof(PoWordProp.KDescr));
str VType = Q(nameof(PoWordProp.VType));
str VDescr = Q(nameof(PoWordProp.VDescr));
str VStr = Q(nameof(PoWordProp.VStr));
str VI64 = Q(nameof(PoWordProp.VI64));
str VF64 = Q(nameof(PoWordProp.VF64));
#endregion PoKv

#region PoLearn
str LearnResult = Q(nameof(PoWordLearn.LearnResult));
#endregion PoLearn

str Sql_WordId = "{WordId}	BLOB REFERENCES {T_Word}({Id}) ON DELETE CASCADE";
str Sql_MkTbl = "{Sql_MkTbl} IF NOT EXISTS";

#region PoBase
	str SqlPoBase =
	$"""
	{Id}	BLOB PRIMARY KEY
	,{CreatedAt}	INT
	,{DbCreatedAt}	INT
	,{UpdatedAt}	INT
	,{DbUpdatedAt}	INT
	,{CreatedBy}	BLOB
	,{LastUpdatedBy}	BLOB
	,{Status}	INT
	""";
#endregion PoBase


#region PoWord
	str T_Word = Q(TblMgr.GetTable<PoWord>().Name);

	Sql=$"""
	{Sql_MkTbl} {T_Word}(
	-- Base
		{SqlPoBase}
	-- ~Base
		,{Owner}	BLOB
		,{Head}	TEXT
		,{Lang}	TEXT
		,UNIQUE({Owner}, {Head}, {Lang})
	);
	CREATE INDEX "IdxWordHeadLang" ON {T_Word} ({Head}, {Lang});
	""";
	Put(Sql);
#endregion PoWord



#region WordProp
	str T_WordProp = Q(TblMgr.GetTable<PoWordProp>().Name);
	Sql=$"""
	{Sql_MkTbl} {T_WordProp}(
	-- PoBase
		{SqlPoBase}
	-- ~PoBase
		,{Sql_WordId}
		,{KType}	INT
		,{KStr}	TEXT
		,{KI64}	INT
		,{KDescr}	TEXT
		,{VType}	INT
		,{VDescr}	TEXT
		,{VStr}	TEXT
		,{VI64}	INT
		,{VF64}	REAL
	)
	CREATE INDEX "IdxKStr" ON {T_WordProp} ({KStr});
	CREATE INDEX "IdxKStr" ON {T_WordProp} ({KI64});
	""";
	Put(Sql);
#endregion


#region WordLearn
	str T_WordLearn = Q(TblMgr.GetTable<PoWordLearn>().Name);
	Sql = $"""
	{Sql_MkTbl} {T_WordLearn}(
		-- PoBase
		{SqlPoBase}
		-- ~PoBase
		,{Sql_WordId}
		,{LearnResult}	TEXT
	)
	""";
	Put(Sql);
#endregion WordLearn

		return str.Join("\n", Segs);

	}
}


