using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using XcaNet.Application.Services;
using XcaNet.Security.DependencyInjection;
using XcaNet.Storage.DependencyInjection;

namespace XcaNet.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSecurityServices();
        services.AddStorageServices();
        services.AddSingleton(configuration);
        services.AddSingleton<IDatabaseSessionService, DatabaseSessionService>();
        return services;
    }
}
