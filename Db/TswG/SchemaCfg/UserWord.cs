using Ngaq.Core.Infra.IF;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.Word.Models;
using Ngaq.Core.Shared.Word.Models.Learn_;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Shared.Word.Models.Po.Learn;
using Ngaq.Core.Shared.Word.Models.Po.Word;

using Ngaq.Core.Word.Models.Po.Word;
using Tsinswreng.CsSql;

namespace Ngaq.Local.Db.TswG;
public partial class LocalTblMgrIniter{
	public static ITblMgr InitWord(ITblMgr Mgr){
		var Tbl_Word = Mk<PoWord>("Word");
		ExtnTblMgr.AddTbl(Mgr, Tbl_Word);
		Mgr.AddTbl(Tbl_Word);
		{
			var o = Tbl_Word;
			CfgPoBase(o);
			CfgBizCreateUpdateTime(o);
			o.Col(x=>x.Id).MapType(IdWord.MkTypeMapFn());
			o.Col(x=>x.Owner).MapType(MapIdUser());
			o.Col(x=>x.StoredAt).MapType(MapTempus());
			// o.InnerAdditionalSqls.AddRange([

			// ]);
			o.IdxExpr(
				null
				,x=>new {x.Head, x.Lang}
				,x=>x.BizCreatedAt
				,x=>x.BizUpdatedAt
				,x=>x.StoredAt
			);
			
			o.IdxExpr(
				new OptMkIdx{Unique = true, Where = o.Tbl.SqlIsNonDel()}
				,x=>new{x.Owner, x.Head, x.Lang}
			);
		}

		var Tbl_Prop = Mk<PoWordProp>("WordProp");
		Mgr.AddTbl(Tbl_Prop);
		{
			var o = Tbl_Prop;
			CfgPoBase(o);
			CfgI_WordId(o);
			CfgIPoKv(o);
			CfgBizCreateUpdateTime(o);
			o.Col(x=>x.Id).MapType(IdWordProp.MkTypeMapFn());

		}

		var Tbl_Learn = Mk<PoWordLearn>("WordLearn");
		Mgr.AddTbl(Tbl_Learn);
		{
			var o = Tbl_Learn;
			CfgPoBase(o);
			CfgI_WordId(o);
			CfgBizCreateUpdateTime(o);
			o.Col(x=>x.Id).MapType(IdWordLearn.MkTypeMapFn());
			//o.SetCol(nameof(PoWordLearn.LearnResult)).MapEnumTypeInt32<ELearn>();
			o.Col(x=>x.LearnResult).MapEnumToStr<ELearn>();
		}

		Mgr.AddAgg(
			AggReg<JnWord, PoWord, IdWord>.Mk(
				Tbl_Word.Tbl
				,x=>x.Id
				,(root, qry)=>new JnWord(
					root
					,qry.GetMany<PoWordProp, IdWord>(root.Id)
					,qry.GetMany<PoWordLearn, IdWord>(root.Id)
				)
			)
			.AddOneToMany(
				Tbl_Prop.Tbl
				,nameof(I_WordId.WordId)
				,x=>x.WordId
			)
			.AddOneToMany(
				Tbl_Learn.Tbl
				,nameof(I_WordId.WordId)
				,x=>x.WordId
			)
		);
		return Mgr;
	}
}
