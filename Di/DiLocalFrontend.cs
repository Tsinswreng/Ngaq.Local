namespace Ngaq.Backend.Di;

using Microsoft.Extensions.DependencyInjection;
using Ngaq.Core.Frontend.ImgBg;
using Ngaq.Core.Frontend.User;
using Ngaq.Core.Frontend.User.Svc;
using Ngaq.Core.Infra.Url;
using Ngaq.Core.Shared.Audio;
using Ngaq.Core.Shared.Dictionary.Svc;
using Ngaq.Core.Shared.Encryption.Svc;
using Ngaq.Core.Shared.User.UserCtx;
using Ngaq.Backend.Domains.Dictionary.Svc;
using Ngaq.Backend.Frontend.Encryption;
using Ngaq.Backend.Frontend.User.Svc;
using Ngaq.Backend.ImplFrontend;
using Ngaq.Backend.Infra;
using Tsinswreng.CsCore;

[Doc("僅限本地(客戶端)後端使用、勿用于Server")]
public static class DiLocalFrontend{
	public static IServiceCollection SetupLocalFrontend(this IServiceCollection z){
		z.AddScoped<ISvcSecretKv, SvcSecretKv>();
		z.AddScoped<ISvcTokenStorage, SvcTokenStorage>();
		z.AddSingleton<IFrontendUserCtxMgr>(FrontendUserCtxMgr.Inst);
		z.AddSingleton<IUserCtx>(FrontendUserCtxMgr.Inst.GetUserCtx());
		z.AddSingleton<I_GetBaseUrl, BaseUrl>();
		
		z.AddSingleton<OnlineAudio>();
		z.AddScoped<ISvcTts, Gtts>();
		z.AddScoped<IImgGetter, SvcImg>();

		return z;
	}
}
