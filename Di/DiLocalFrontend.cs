namespace Ngaq.Local.Di;

using Microsoft.Extensions.DependencyInjection;
using Ngaq.Core.Shared.Encryption.Svc;
using Ngaq.Local.Domains.Encryption;

public static class DiLocalFrontend{
	public static IServiceCollection SetupLocalFrontend(this IServiceCollection z){
		z.AddScoped<ISvcSecretKv, SvcSecretKv>();
		return z;
	}
}
