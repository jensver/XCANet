using Microsoft.Extensions.DependencyInjection;
using XcaNet.Application.Services;

namespace XcaNet.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IStartupWorkflow, StartupWorkflow>();
        return services;
    }
}
