using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XcaNet.Contracts.Crypto;
using XcaNet.Crypto.Abstractions;
using XcaNet.Crypto.DotNet;
using XcaNet.Interop.OpenSsl;

namespace XcaNet.Crypto.OpenSsl.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddXcaNetCryptoServices(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<CryptoBackendRoutingOptions>? configure = null)
    {
        var options = new CryptoBackendRoutingOptions();
        if (configuration is not null)
        {
            if (Enum.TryParse<CryptoBackendPreference>(configuration["Crypto:DefaultPreference"], true, out var defaultPreference))
            {
                options.DefaultPreference = defaultPreference;
            }

            options.OpenSslBridgePath = configuration["Crypto:OpenSslBridgePath"];
        }

        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<DotNetCryptoBackend>();
        services.AddSingleton<IOpenSslBridgeClient>(_ => new OpenSslBridgeClient(new OpenSslBridgeOptions
        {
            LibraryPath = options.OpenSslBridgePath
        }));
        services.AddSingleton<OpenSslCryptoBackend>();
        services.AddSingleton<IKeyService>(provider => provider.GetRequiredService<DotNetCryptoBackend>());
        services.AddSingleton<ICertificateSigningRequestService>(provider => provider.GetRequiredService<DotNetCryptoBackend>());
        services.AddSingleton<IImportExportService>(provider => provider.GetRequiredService<DotNetCryptoBackend>());
        services.AddSingleton<ICertificateService, RoutedCertificateService>();
        services.AddSingleton<ICryptoBackendDiagnosticsProvider, RoutedCryptoBackendDiagnosticsProvider>();
        return services;
    }
}
