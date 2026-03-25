namespace Ngaq.Local.Word.Dao;

using Ngaq.Core.Infra.IF;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Shared.Base.Models.Po;
using Ngaq.Core.Shared.User.Models.Po.User;
using Ngaq.Core.Shared.Word.Models;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Shared.Word.Models.Po.Learn;
using Ngaq.Core.Shared.Word.Models.Po.Word;
using Ngaq.Core.Word.Models.Po.Word;
using Ngaq.Local.Db.TswG;
using Tsinswreng.CsCore;
using Tsinswreng.CsSql;
using Tsinswreng.CsTools;

public partial class DaoWordV2(
	ISqlCmdMkr SqlCmdMkr
	,ITblMgr TblMgr
	,IRepo<PoWord, IdWord> RepoWord
	,IRepo<PoWordProp, IdWordProp> RepoProp
	,IRepo<PoWordLearn, IdWordLearn> RepoLearn
){
	ITable<PoWord> TW => TblMgr.GetTbl<PoWord>();
	ITable<PoWordProp> TP => TblMgr.GetTbl<PoWordProp>();
	ITable<PoWordLearn> TL => TblMgr.GetTbl<PoWordLearn>();

	public Task<nil> BatAltWordAfterUpd(
		IDbFnCtx Ctx, IAsyncEnumerable<IdWord> Ids, CT Ct
	){
		return RepoWord.AsAppRepo().BatBizTouch(Ctx, Ids, Ct);
	}

	public IAsyncEnumerable<PoWord?> BatGetPoWordByIdWithDel(
		IDbFnCtx Ctx
		,IAsyncEnumerable<IdWord> Ids
		,CT Ct
	){
		return RepoWord.BatGetByIdWithDel(Ctx, Ids, Ct);
	}

	public IAsyncEnumerable<JnWord?> BatGetJnWordByIdWithDel(
		IDbFnCtx Ctx
		,IAsyncEnumerable<IdWord> Ids
		,CT Ct
	){
		return RepoWord.BatGetAggByIdWithDel<JnWord>(Ctx, Ids, Ct);
	}

	public IAsyncEnumerable<IdWord?> BatSlctIdByOwnerHeadLangWithDel(
		IDbFnCtx Ctx
		,IdUser Owner
		,IAsyncEnumerable<Head_Lang> HeadLangs
		,CT Ct
	){
		var Heads = HeadLangs.Select(x=>x.Head);
		var Langs = HeadLangs.Select(x=>x.Lang);
		var Sql = TW.SqlSplicer().Select(x=>x.Id).From().Where1()
		.AndEq(x=>x.Owner, y=>y.One(Owner))
		.AndEq(x=>x.Head, y=>y.Many(Heads))
		.AndEq(x=>x.Lang, y=>y.Many(Langs))
		;
		var Dicts = SqlCmdMkr.RunDupliSql(Ctx, Sql, Ct);
		return Dicts.Select(x=>{
			if(x is null){
				return null;
			}
			var RawId = x[TW.Memb(e=>e.Id)];
			return (IdWord?)IdWord.FromByteArr((u8[])RawId!);
		});
	}

	public IAsyncEnumerable<JnWord> GetWordsByOwner(
		IDbFnCtx Ctx
		,IdUser Owner
		,CT Ct
	){
		var Sql = TW.SqlSplicer().Select("*").From().Where1()
		.And(TW.SqlIsNonDel())
		.AndEq(x=>x.Owner, y=>y.One(Owner))
		.OrderByDesc(x=>x.BizCreatedAt)
		;
		var PoWords = SqlCmdMkr.RunDupliSql(Ctx, TW, Sql, Ct)
		.Where(x=>x is not null)
		.Select(x=>x!)
		;

		return CollectJnWordsByBatch(Ctx, PoWords, Ct);
	}

	IAsyncEnumerable<JnWord> CollectJnWordsByBatch(
		IDbFnCtx Ctx
		,IAsyncEnumerable<PoWord> PoWords
		,CT Ct
	){
		var Batch = new BatchCollector<PoWord, IList<JnWord>>(async(words, Ct)=>{
			var Ids = words.Select(x=>x.Id).ToList();
			if(Ids.Count == 0){
				return [];
			}
			var NWordId = nameof(I_WordId.WordId);
			var Opt = new OptQry{
				InParamCnt = (u64)Ids.Count,
				IncludeDeleted = false,
			};
			var PropsById = await RepoWord.IncludeEntitysByKeys(Ctx, NWordId, Opt, Ids, x=>x.WordId, TP, Ct);
			var LearnsById = await RepoWord.IncludeEntitysByKeys(Ctx, NWordId, Opt, Ids, x=>x.WordId, TL, Ct);

			IList<JnWord> R = [];
			foreach(var PoWord in words){
				var Props = PropsById.GetValueOrDefault(PoWord.Id, []);
				var Learns = LearnsById.GetValueOrDefault(PoWord.Id, []);
				R.Add(new JnWord(PoWord, Props, Learns));
			}
			return R;
		});
		var S2D = Batch.AddToEnd(PoWords, Ct);
		return S2D.Flat();
	}
}
