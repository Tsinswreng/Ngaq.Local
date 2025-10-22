namespace Ngaq.Local.Di;

using Microsoft.Extensions.DependencyInjection;
using Ngaq.Core.Frontend.User.Svc;
using Ngaq.Core.Shared.Encryption.Svc;
using Ngaq.Local.Domains.Encryption;
using Ngaq.Local.Frontend.User.Svc;

public static class DiLocalFrontend{
	public static IServiceCollection SetupLocalFrontend(this IServiceCollection z){
		z.AddScoped<ISvcSecretKv, SvcSecretKv>();
		z.AddScoped<ISvcTokenStorage, SvcTokenStorage>();
		return z;
	}
}
