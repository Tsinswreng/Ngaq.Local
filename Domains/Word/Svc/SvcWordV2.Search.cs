namespace Ngaq.Backend.Domains.Word.Svc;

using Ngaq.Core.Infra;
using Ngaq.Core.Infra.IF;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Model.Po.Learn_;
using Ngaq.Core.Shared.Base.Models.Po;
using Ngaq.Core.Shared.Word.Models;
using Ngaq.Core.Shared.Word.Models.Dto;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Ngaq.Core.Shared.Word.Models.Po.Learn;
using Ngaq.Core.Shared.Word.Models.Po.Word;
using Tsinswreng.CsPage;
using Tsinswreng.CsSql;

public partial class SvcWordV2{
	/// 搜索當前用戶詞庫。當前按“先精確命中、後詞頭前綴命中”的順序組裝結果，
	/// 並在服務層完成命中信息封裝與分頁裁切。
	public async Task<IPage<DtoWordSearchHit>> PageSearch(
		IDbUserCtx Ctx,
		IPageQry PageQry,
		ReqSearchWord Req,
		CT Ct
	){
		Ctx.DbFnCtx ??= new DbFnCtx();
		var IdHits = await GetExactSearchHits(Ctx, Req, Ct);
		if(IdHits.Count > 0){
			return BuildPageSearchResult(PageQry, IdHits);
		}

		var RawStr = NormalizeRawStr(Req.RawStr);
		var ExactHeadPage = await DaoWordV2.PageWordsByOwnerAndHead(
			Ctx.DbFnCtx,
			Ctx.UserCtx.UserId,
			PageQry,
			RawStr,
			IsExact: true,
			Ct
		);
		if(ExactHeadPage.TotCnt > 0){
			return await ToWordHitPage(PageQry, ExactHeadPage, Ct);
		}

		var FuzzyHeadPage = await DaoWordV2.PageWordsByOwnerAndHead(
			Ctx.DbFnCtx,
			Ctx.UserCtx.UserId,
			PageQry,
			RawStr,
			IsExact: false,
			Ct
		);
		return await ToWordHitPage(PageQry, FuzzyHeadPage, Ct);
	}

	async Task<List<DtoWordSearchHit>> GetExactSearchHits(
		IDbUserCtx Ctx,
		ReqSearchWord Req,
		CT Ct
	){
		List<DtoWordSearchHit> Hits = [];
		var RawStr = NormalizeRawStr(Req.RawStr);
		if(RawStr == ""){
			return Hits;
		}

		if(TryParseWordId(RawStr, out var WordId)){
			var WordHit = await TryGetWordHit(Ctx, WordId, Ct);
			if(WordHit is not null){
				Hits.Add(WordHit);
			}
		}
		if(TryParseWordPropId(RawStr, out var PropId)){
			var PropHit = await TryGetPropHit(Ctx, PropId, Ct);
			if(PropHit is not null){
				Hits.Add(PropHit);
			}
		}
		if(TryParseWordLearnId(RawStr, out var LearnId)){
			var LearnHit = await TryGetLearnHit(Ctx, LearnId, Ct);
			if(LearnHit is not null){
				Hits.Add(LearnHit);
			}
		}
		return Hits;
	}

	async Task<DtoWordSearchHit?> TryGetWordHit(
		IDbUserCtx Ctx,
		IdWord WordId,
		CT Ct
	){
		var Word = await GetJnWordByIdAndCheckOwner(Ctx, WordId, Ct);
		if(Word is null){
			return null;
		}
		return MkWordHit(Word);
	}

	async Task<DtoWordSearchHit?> TryGetPropHit(
		IDbUserCtx Ctx,
		IdWordProp PropId,
		CT Ct
	){
		var Prop = await RepoProp.OrdGetById(Ctx.DbFnCtx!, ToAsyE([PropId]), Ct).FirstOrDefaultAsync(Ct);
		if(Prop is null){
			return null;
		}

		var Word = await GetJnWordByIdAndCheckOwner(Ctx, Prop.WordId, Ct);
		if(Word is null){
			return null;
		}
		return new DtoWordSearchHit{
			JnWord = Word,
			HitKind = EWordSearchHitKind.WordProp,
			WordProp = Prop,
		};
	}

	async Task<DtoWordSearchHit?> TryGetLearnHit(
		IDbUserCtx Ctx,
		IdWordLearn LearnId,
		CT Ct
	){
		var Learn = await RepoLearn.OrdGetById(Ctx.DbFnCtx!, ToAsyE([LearnId]), Ct).FirstOrDefaultAsync(Ct);
		if(Learn is null){
			return null;
		}

		var Word = await GetJnWordByIdAndCheckOwner(Ctx, Learn.WordId, Ct);
		if(Word is null){
			return null;
		}
		return new DtoWordSearchHit{
			JnWord = Word,
			HitKind = EWordSearchHitKind.WordLearn,
			WordLearn = Learn,
		};
	}

	async Task<JnWord?> GetJnWordByIdAndCheckOwner(
		IDbUserCtx Ctx,
		IdWord WordId,
		CT Ct
	){
		var Word = await RepoWord.OrdGetAggById<JnWord>(Ctx.DbFnCtx!, ToAsyE([WordId]), Ct).FirstOrDefaultAsync(Ct);
		if(Word is null){
			return null;
		}
		Word.Word.CheckOwner(Ctx.UserCtx.UserId);
		return Word;
	}

	static DtoWordSearchHit MkWordHit(JnWord Word){
		return new DtoWordSearchHit{
			JnWord = Word,
			HitKind = EWordSearchHitKind.Word,
		};
	}

	/// StronglyTypedId 當前只穩定提供 FromLow64Base；服務層這裏包一層 Try，避免把解析異常外漏成搜索失敗。
	static bool TryParseWordId(str? Raw, out IdWord Id){
		if(string.IsNullOrWhiteSpace(Raw)){
			Id = default;
			return false;
		}
		try{
			Id = IdWord.FromLow64Base(Raw);
			return true;
		}catch{
			Id = default;
			return false;
		}
	}

	/// PropId 搜索和 WordId 同策略：能按 Low64Base 解析才視爲精確命中。
	static bool TryParseWordPropId(str? Raw, out IdWordProp Id){
		if(string.IsNullOrWhiteSpace(Raw)){
			Id = default;
			return false;
		}
		try{
			Id = IdWordProp.FromLow64Base(Raw);
			return true;
		}catch{
			Id = default;
			return false;
		}
	}

	/// LearnId 搜索和 WordId/PropId 保持一致，避免三種精確命中行爲不一致。
	static bool TryParseWordLearnId(str? Raw, out IdWordLearn Id){
		if(string.IsNullOrWhiteSpace(Raw)){
			Id = default;
			return false;
		}
		try{
			Id = IdWordLearn.FromLow64Base(Raw);
			return true;
		}catch{
			Id = default;
			return false;
		}
	}

	static str NormalizeRawStr(str? RawStr){
		return (RawStr ?? "").Trim();
	}

	async Task<IPage<DtoWordSearchHit>> ToWordHitPage(
		IPageQry Qry,
		IPageAsyE<JnWord> WordPage,
		CT Ct
	){
		List<DtoWordSearchHit> Data = [];
		if(WordPage.DataAsyE is not null){
			await foreach(var Word in WordPage.DataAsyE.WithCancellation(Ct)){
				Data.Add(MkWordHit(Word));
			}
		}
		return Tsinswreng.CsPage.Page.Mk(
			Qry,
			Data,
			HasTotalCount: Qry.WantTotCnt,
			TotalCount: Qry.WantTotCnt ? WordPage.TotCnt : 0
		);
	}

	static IPage<DtoWordSearchHit> BuildPageSearchResult(
		IPageQry Qry,
		IList<DtoWordSearchHit> Rows
	){
		var Offset = Qry.PageIdx * Qry.PageSize;
		var Limit = Offset + Qry.PageSize;
		List<DtoWordSearchHit> Data = [];
		for(u64 i = 0; i < (u64)Rows.Count; i++){
			if(i < Offset){
				continue;
			}
			if(i >= Limit){
				break;
			}
			Data.Add(Rows[(int)i]);
		}

		var TotalCount = (u64)Rows.Count;
		return Page.Mk(
			Qry,
			Data,
			HasTotalCount: Qry.WantTotCnt,
			TotalCount: Qry.WantTotCnt ? TotalCount : 0
		);
	}
}
