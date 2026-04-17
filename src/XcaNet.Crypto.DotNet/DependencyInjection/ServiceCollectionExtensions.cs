using Microsoft.Extensions.DependencyInjection;
using XcaNet.Crypto.Abstractions;

namespace XcaNet.Crypto.DotNet.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddManagedCryptoServices(this IServiceCollection services)
    {
        services.AddSingleton<DotNetCryptoBackend>();
        services.AddSingleton<IKeyService>(provider => provider.GetRequiredService<DotNetCryptoBackend>());
        services.AddSingleton<ICertificateService>(provider => provider.GetRequiredService<DotNetCryptoBackend>());
        services.AddSingleton<ICertificateSigningRequestService>(provider => provider.GetRequiredService<DotNetCryptoBackend>());
        services.AddSingleton<IImportExportService>(provider => provider.GetRequiredService<DotNetCryptoBackend>());
        return services;
    }
}
