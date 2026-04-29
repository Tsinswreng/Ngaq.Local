using Ngaq.Core.Shared.Audio;
using Ngaq.Core.Infra.Errors;
using Ngaq.Core.Shared.Dictionary.Svc;
using Tsinswreng.CsErr;
using Tsinswreng.CsCore;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;
using Ngaq.Core.Shared.Dictionary.Models;

namespace Ngaq.Backend.Domains.Dictionary.Svc;


/// Google TTS 服務實現。
/// 輸入文本與標準語言 Id，返回可重讀的音頻數據。
public class Gtts: ISvcTts{
	/// Google Translate TTS 端點。
	const str GttsApiUrl = "https://translate.google.com/translate_tts";
	/// 客戶端標識；遵循公開常見調用方式。
	const str GttsClient = "tw-ob";
	/// 編碼參數固定為 UTF-8。
	const str GttsInputEncoding = "UTF-8";

	/// 在線音頻下載器：把 URL 下載並封裝為可重讀 Audio。
	private readonly OnlineAudio OnlineAudio;
	/// 內存緩存：同一 (文本, 語言碼) 只下載一次。
	private readonly ConcurrentDictionary<str, Lazy<Task<Audio>>> Cache = [];

	public Gtts(
		OnlineAudio OnlineAudio
	){
		this.OnlineAudio = OnlineAudio;
	}

	/// 根據文本和標準語言生成語音。
	/// <param name="Text">要轉語音的文本。</param>
	/// <param name="Lang">標準語言（含 Type/Code）。</param>
	/// <returns>可重複讀取的音頻對象。</returns>
	public Task<Audio> GetAudio(
		str Text, INormLang Lang
	){
		// step 1: 先做入參校驗，避免下游查庫/請求時才報錯。
		if(str.IsNullOrWhiteSpace(Text)){
			throw KeysErr.Common.ArgErr.ToErr().AddDebugArgs(nameof(Text));
		}
		if(Lang is null){
			throw KeysErr.Common.ArgErr.ToErr().AddDebugArgs(nameof(Lang));
		}

		return GetAudioCore(Text, Lang);
	}

	/// 真正的異步流程：讀語言碼 -> 命中/寫入緩存 -> 下載音頻。
	private async Task<Audio> GetAudioCore(str Text, INormLang Lang){
		// step 2: 直接使用接口提供的標準語言碼，避免額外查庫。
		var gttsLangCode = NormalizeLangCodeForGtts(Lang.Code);
		if(str.IsNullOrWhiteSpace(gttsLangCode)){
			throw KeysErr.Common.ArgErr.ToErr().AddDebugArgs(nameof(Lang.Code), Lang.Code);
		}

		// step 3: 以「文本+語言碼」作為鍵進行內存緩存，減少重複下載。
		var key = BuildCacheKey(Text, gttsLangCode);
		var lazy = Cache.GetOrAdd(
			key,
			_=>new Lazy<Task<Audio>>(
				()=>LoadAudio(Text, gttsLangCode),
				LazyThreadSafetyMode.ExecutionAndPublication
			)
		);

		// step 4: 若此次下載失敗，移除壞緩存，讓後續請求可重試。
		try{
			return await lazy.Value;
		}
		catch{
			Cache.TryRemove(key, out _);
			throw;
		}
	}

	/// 組裝 gTTS URL 並下載音頻。
	private Task<Audio> LoadAudio(str Text, str GttsLangCode){
		var url = BuildGttsUrl(Text, GttsLangCode);
		var req = BuildGttsRequest(url, GttsLangCode);
		return OnlineAudio.Get(req);
	}

	/// 生成 Google TTS 調用地址。
	private static str BuildGttsUrl(str Text, str GttsLangCode){
		var encodedText = Uri.EscapeDataString(Text);
		var encodedLang = Uri.EscapeDataString(GttsLangCode);
		return $"{GttsApiUrl}?ie={GttsInputEncoding}&q={encodedText}&tl={encodedLang}&client={GttsClient}";
	}

	/// 把倉庫中的語言碼歸一化成 gTTS 可接受格式。
	/// 例如 `zh_hant_tw` -> `zh-hant-tw`。
	private static str NormalizeLangCodeForGtts(str? Code){
		return (Code ?? "").Trim().Replace('_', '-');
	}

	/// 構造緩存鍵。
	private static str BuildCacheKey(str Text, str GttsLangCode){
		return $"{GttsLangCode}\n{Text}";
	}

	/// <summary>
	/// gTTS 對「不像瀏覽器的匿名程序請求」容易直接回 429。
	/// 因此這裡顯式補上常見瀏覽器請求頭，讓 Android / 桌面端行爲更接近手動瀏覽器訪問。
	/// </summary>
	private static HttpRequestMessage BuildGttsRequest(str Url, str GttsLangCode){
		var req = new HttpRequestMessage(HttpMethod.Get, Url);
		req.Headers.UserAgent.ParseAdd(
			"Mozilla/5.0 (Linux; Android 14; Mobile) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Mobile Safari/537.36"
		);
		req.Headers.Referrer = new Uri("https://translate.google.com/");
		req.Headers.Accept.ParseAdd("audio/mpeg,audio/*;q=0.9,*/*;q=0.8");
		req.Headers.AcceptLanguage.ParseAdd($"{GttsLangCode},en;q=0.9");
		req.Headers.TryAddWithoutValidation("Origin", "https://translate.google.com");
		return req;
	}
}
