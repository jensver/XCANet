using Microsoft.Extensions.DependencyInjection;
using XcaNet.Security.Protection;

namespace XcaNet.Security.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSecurityServices(this IServiceCollection services)
    {
        services.AddSingleton<IDatabaseSecretProtector, DatabaseSecretProtector>();
        return services;
    }
}
