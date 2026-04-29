using Ngaq.Core.Infra;
using Ngaq.Core.Infra.Cfg;
using Ngaq.Core.Infra.Errors;
using Ngaq.Core.Shared.Base.Models.Po;
using Ngaq.Core.Shared.Dictionary.Models;
using Ngaq.Core.Shared.Dictionary.Models.Po.NormLang;
using Ngaq.Core.Shared.Word.Models.Dto;
using Ngaq.Core.Shared.Word.Svc;
using Ngaq.Backend.Db.TswG;
using Ngaq.Backend.Domains.Word.Dao;
using Tsinswreng.CsErr;
using Tsinswreng.CsI18n;
using Tsinswreng.CsPage;
using Tsinswreng.CsSql;
using Tsinswreng.CsTools;
using System;
using System.Collections.Generic;
using System.Linq;
using Tsinswreng.CsCfg;
using Tsinswreng.CsCore;

namespace Ngaq.Backend.Domains.Word.Svc;

public class SvcNormLang : ISvcNormLang{
	private readonly DaoNormLang Dao;
	private readonly ISqlCmdMkr SqlCmdMkr;
	private readonly IRepo<PoNormLang, IdNormLang> RepoNormLang;
	private readonly II18n? I18n;
	private readonly Tsinswreng.CsCfg.ICfgAccessor Cfg;

	public SvcNormLang(
		DaoNormLang Dao,
		ISqlCmdMkr SqlCmdMkr,
		IRepo<PoNormLang, IdNormLang> RepoNormLang,
		Tsinswreng.CsCfg.ICfgAccessor Cfg,
		II18n? I18n = null
	){
		this.Dao = Dao;
		this.SqlCmdMkr = SqlCmdMkr;
		this.RepoNormLang = RepoNormLang;
		this.Cfg = Cfg;
		this.I18n = I18n;
	}

	public IAsyncEnumerable<PoNormLang?> BatGetNormLangByTypeCode(
		IDbUserCtx Ctx,
		IAsyncEnumerable<(ELangIdentType, str)> Type_Code,
		CT Ct
	){
		Ctx.DbFnCtx ??= new DbFnCtx();
		return Dao.BatGetNormLangByTypeCode(Ctx.DbFnCtx, Ctx.UserCtx.UserId, Type_Code, Ct);
	}

	/// 批量獲取翻譯名稱。返回順序與入參語言順序一一對應。
	public async IAsyncEnumerable<str?> BatGetTranslatedName(
		IDbUserCtx Ctx,
		INormLang TargetLang,
		IAsyncEnumerable<INormLang> NormLangs,
		[System.Runtime.CompilerServices.EnumeratorCancellation] CT Ct
	){
		_ = Ctx;
		await foreach(var NormLang in NormLangs.WithCancellation(Ct)){
			yield return GetTranslatedName(TargetLang, NormLang);
		}
	}

	[Doc(@$"按 以下優先級 模糊查詢:
	- {nameof(PoNormLang.Code)}
	- {nameof(PoNormLang.NativeName)}
	- TranslatedName

	按 {nameof(PoNormLang.Weight)} 降序。
	數量不大，允許全部加載入內存中篩選。")]
	public async Task<IPageAsyE<PoNormLang>> PageNormLang(
		IDbUserCtx Ctx,
		ReqPageNormLang Req,
		CT Ct
	){
		Ctx.DbFnCtx ??= new DbFnCtx();
		var SearchText = NormalizeSearchText(Req.SearchText);
		if(string.IsNullOrWhiteSpace(SearchText)){
			return await Dao.PageNormLang(Ctx.DbFnCtx, Ctx.UserCtx.UserId, Req, Ct);
		}

		// 需同時按翻譯名稱查詢，故先按最終排序規則取全集，再在內存做匹配與分頁。
		var AllReq = new ReqPageNormLang{
			PageQry = PageQry.SlctI64Max(),
			SearchText = null,
		};
		var AllPage = await Dao.PageNormLang(Ctx.DbFnCtx, Ctx.UserCtx.UserId, AllReq, Ct);
		var AllRows = await CollectRows(AllPage.DataAsyE, Ct);
		if(AllRows.Count == 0){
			return Req.PageQry.ToPageAsyE(ToolAsyE.ToAsyE(Array.Empty<PoNormLang>()), Req.PageQry.WantTotCnt, 0);
		}

		var TargetLang = MkTargetLang();
		var TranslatedNames = BatGetTranslatedName(
			Ctx,
			TargetLang,
			ToolAsyE.ToAsyE(AllRows.Select(ToNormLang)),
			Ct
		);
		var Matched = await FilterBySearchTextByPriority(AllRows, TranslatedNames, SearchText, Ct);
		return BuildPageResult(Req.PageQry, Matched);
	}

	/// 取 UI 語言候選。僅返回少量常用語言，供設置頁下拉選擇。
	public IAsyncEnumerable<INormLangDetail> BatGetUiLangs(CT CT){
		_ = CT;
		var Rows = new INormLangDetail[]{
			MkUiLang("en", "English", "English", 100),
			MkUiLang("zh-CN", "简体中文", "Chinese (Simplified)", 99),
			MkUiLang("zh-TW", "繁體中文", "Chinese (Traditional)", 98),
			MkUiLang("ja", "日本語", "Japanese", 97),
			MkUiLang("ko", "한국어", "Korean", 96),
			MkUiLang("es", "Español", "Spanish", 95),
			MkUiLang("fr", "Français", "French", 94),
			MkUiLang("de", "Deutsch", "German", 93),
			MkUiLang("ru", "Русский", "Russian", 92),
			MkUiLang("pt", "Português", "Portuguese", 85),
			MkUiLang("it", "Italiano", "Italian", 80),
			MkUiLang("vi", "Tiếng Việt", "Vietnamese", 50),
			MkUiLang("th", "ภาษาไทย", "Thai", 50),
		};
		return ToolAsyE.ToAsyE(Rows);
	}

	public async Task<nil> BatAddNormLang(
		IDbUserCtx Ctx,
		IAsyncEnumerable<PoNormLang> Pos,
		CT Ct
	) => await DoWrite(Ctx, Pos, needTouch: true, async (DbCtx, Repo, Ents, Ct2)=>{
		await Repo.BatAdd(DbCtx, Ents, Ct2);
	}, Ct);

	public async Task<nil> BatUpdNormLang(
		IDbUserCtx Ctx,
		IAsyncEnumerable<PoNormLang> Pos,
		CT Ct
	) => await DoWrite(Ctx, Pos, needTouch: false, async (DbCtx, Repo, Ents, Ct2)=>{
		await Repo.BatUpd(DbCtx, Ents, Ct2);
		await Repo.AsAppRepo().BatBizTouch(DbCtx, Ents.Select(x=>x.Id), Ct2);
	}, Ct);

	public async Task<nil> BatSoftDelNormLang(
		IDbUserCtx Ctx,
		IAsyncEnumerable<PoNormLang> Pos,
		CT Ct
	) => await DoWrite(Ctx, Pos, needTouch: false, async (DbCtx, Repo, Ents, Ct2)=>{
		await Repo.BatSoftDelById(DbCtx, Ents.Select(x=>x.Id), Ct2);
	}, Ct);

	public async Task<nil> InitBuiltinNormLang(IDbUserCtx Ctx, CT Ct){
		var Owner = Ctx.UserCtx.UserId;
		var SeedList = InitNormLang.GetNormLangList();
		if(SeedList.Count == 0){
			return NIL;
		}

		var Exists = BatGetNormLangByTypeCode(
			Ctx,
			ToolAsyE.ToAsyE(SeedList.Select(x=>(x.Type, x.Code))),
			Ct
		);

		// 對齊批量查詢返回的順序，只保留數據庫中尚不存在的內置語言。
		async IAsyncEnumerable<PoNormLang> FilterNonExists(){
			var i = 0;
			await foreach(var Po in Exists.WithCancellation(Ct)){
				if(i >= SeedList.Count){
					yield break;
				}
				if(Po is null){
					var o = SeedList[i];
					yield return new PoNormLang{
						Owner = Owner,
						Type = o.Type,
						Code = o.Code,
						NativeName = o.NativeName,
						EnglishName = o.EnglishName,
						Weight = o.Weight,
					};
				}
				i++;
			}
		}

		await BatAddNormLang(Ctx, FilterNonExists(), Ct);
		return NIL;
	}

	private async Task<nil> DoWrite(
		IDbUserCtx Ctx,
		IAsyncEnumerable<PoNormLang> Pos,
		bool needTouch,
		Func<IDbFnCtx, IRepo<PoNormLang, IdNormLang>, IAsyncEnumerable<PoNormLang>, CT, Task> Operation,
		CT Ct
	){
		Pos = Pos.CheckOwner(Ctx.UserCtx.UserId);
		if(needTouch){
			Pos = Pos.Touch();
		}
		try{
			await SqlCmdMkr.EnsureTxn(Ctx.DbFnCtx, Ct, async DbCtx=>{
				await Operation(DbCtx, RepoNormLang, Pos, Ct);
				return NIL;
			});
			return NIL;
		}
		catch(AppErr){
			throw;
		}
		catch(Exception Ex){
			var E = KeysErr.Common.DataIllegalOrConflict.ToErr();
			E.AddErr(Ex);
			throw E;
		}
	}

	/// 構造目標語言。當前 i18n 實例由宿主初始化，這裏僅保留接口語義。
	private INormLang MkTargetLang(){
		var LangCode = KeysClientCfg.Lang.GetFrom(Cfg) ?? "en";
		return new NormLang{
			Type = ELangIdentType.Bcp47,
			Code = LangCode,
		};
	}

	/// 單個語言的翻譯顯示名。格式爲 語言名 或 語言名(地區名)。
	private str? GetTranslatedName(INormLang TargetLang, INormLang NormLang){
		_ = TargetLang;
		var Code = (NormLang.Code ?? "").Trim();
		if(Code == ""){
			return null;
		}
		var Parsed = ParseBcp47(Code);
		var LangName = GetI18nText(["Lang", Parsed.Lang], Parsed.Lang);
		if(LangName == ""){
			LangName = Code;
		}
		if(Parsed.Region is null){
			return LangName;
		}
		var RegionName = GetI18nText(["Region", Parsed.Region], Parsed.Region);
		if(RegionName == ""){
			return LangName;
		}
		return $"{LangName}({RegionName})";
	}

	private str GetI18nText(IList<str> PathSegs, str Fallback){
		if(I18n is null){
			return Fallback;
		}
		var Key = I18nKey.Mk(null, PathSegs);
		var Value = I18n.Get(Key);
		return string.IsNullOrWhiteSpace(Value) ? Fallback : Value;
	}

	private static async Task<List<PoNormLang>> CollectRows(
		IAsyncEnumerable<PoNormLang>? Rows,
		CT Ct
	){
		List<PoNormLang> Ans = [];
		if(Rows is null){
			return Ans;
		}
		await foreach(var Po in Rows.WithCancellation(Ct)){
			Ans.Add(Po);
		}
		return Ans;
	}

	/// 按 Code/NativeName/TranslatedName 的優先級分組匹配；組內保持原有 Weight 降序。
	private static async Task<List<PoNormLang>> FilterBySearchTextByPriority(
		IList<PoNormLang> Rows,
		IAsyncEnumerable<str?> TranslatedNames,
		str SearchText,
		CT Ct
	){
		List<PoNormLang> CodeMatched = [];
		List<PoNormLang> NativeNameMatched = [];
		List<PoNormLang> TranslatedNameMatched = [];
		var Idx = 0;
		await foreach(var TranslatedName in TranslatedNames.WithCancellation(Ct)){
			if(Idx >= Rows.Count){
				break;
			}
			var Po = Rows[Idx];
			switch(GetMatchPriority(Po, TranslatedName, SearchText)){
				case EMatchPriority.Code:
					CodeMatched.Add(Po);
					break;
				case EMatchPriority.NativeName:
					NativeNameMatched.Add(Po);
					break;
				case EMatchPriority.TranslatedName:
					TranslatedNameMatched.Add(Po);
					break;
				default:
					break;
			}
			Idx++;
		}
		List<PoNormLang> Ans = [
			..CodeMatched,
			..NativeNameMatched,
			..TranslatedNameMatched,
		];
		return Ans;
	}

	private static IPageAsyE<PoNormLang> BuildPageResult(
		IPageQry Qry,
		IList<PoNormLang> Rows
	){
		var Ofst = Qry.PageIdx * Qry.PageSize;
		var Lim = Ofst + Qry.PageSize;
		async IAsyncEnumerable<PoNormLang> Yield(){
			for(u64 i = 0; i < (u64)Rows.Count; i++){
				if(i < Ofst){
					continue;
				}
				if(i >= Lim){
					yield break;
				}
				yield return Rows[(int)i];
			}
		}
		var TotCnt = (u64)Rows.Count;
		return Qry.ToPageAsyE(
			Yield(),
			HasTotCnt: Qry.WantTotCnt,
			TotCnt: Qry.WantTotCnt ? TotCnt : 0
		);
	}

	/// 返回首個命中的字段優先級，確保同一條記錄只落入一個優先級桶。
	private static EMatchPriority GetMatchPriority(PoNormLang Po, str? TranslatedName, str SearchText){
		if(ContainsIgnoreCase(Po.Code, SearchText)){
			return EMatchPriority.Code;
		}
		if(ContainsIgnoreCase(Po.NativeName, SearchText)){
			return EMatchPriority.NativeName;
		}
		if(ContainsIgnoreCase(TranslatedName, SearchText)){
			return EMatchPriority.TranslatedName;
		}
		return EMatchPriority.None;
	}

	private static bool ContainsIgnoreCase(str? Text, str SearchText){
		if(string.IsNullOrWhiteSpace(Text)){
			return false;
		}
		return Text.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static str NormalizeSearchText(str? SearchText){
		return (SearchText ?? "").Trim();
	}

	private static INormLang ToNormLang(PoNormLang Po){
		return new NormLang{
			Type = Po.Type,
			Code = Po.Code,
		};
	}

	/// 搜索命中優先級。數值順序即業務要求的返回優先級。
	private enum EMatchPriority{
		None = 0,
		Code = 1,
		NativeName = 2,
		TranslatedName = 3,
	}

	private sealed class Bcp47Parts{
		public str Lang{get;set;} = "";
		public str? Region{get;set;} = null;
	}

	private static Bcp47Parts ParseBcp47(str Code){
		var Segs = Code.Split('-', StringSplitOptions.RemoveEmptyEntries);
		if(Segs.Length == 0){
			return new Bcp47Parts();
		}
		var Ans = new Bcp47Parts{
			Lang = Segs[0].ToLowerInvariant(),
		};
		for(var i = 1; i < Segs.Length; i++){
			if(IsRegionSeg(Segs[i])){
				Ans.Region = Segs[i].ToUpperInvariant();
				break;
			}
		}
		return Ans;
	}

	private static bool IsRegionSeg(str Seg){
		if(Seg.Length == 2){
			return Seg.All(char.IsLetter);
		}
		if(Seg.Length == 3){
			return Seg.All(char.IsDigit);
		}
		return false;
	}

	private static INormLangDetail MkUiLang(
		str Code,
		str NativeName,
		str EnglishName,
		f64 Weight
	){
		return new NormLangDetail{
			Type = ELangIdentType.Bcp47,
			Code = Code,
			NativeName = NativeName,
			EnglishName = EnglishName,
			Weight = Weight,
		};
	}
}
