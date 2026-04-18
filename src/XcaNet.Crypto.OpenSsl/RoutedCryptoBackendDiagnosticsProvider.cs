using XcaNet.Contracts.Crypto;
using XcaNet.Crypto.Abstractions;
using XcaNet.Interop.OpenSsl;

namespace XcaNet.Crypto.OpenSsl;

public sealed class RoutedCryptoBackendDiagnosticsProvider : ICryptoBackendDiagnosticsProvider
{
    private readonly CryptoBackendRoutingOptions _options;
    private readonly IOpenSslBridgeClient _bridgeClient;

    public RoutedCryptoBackendDiagnosticsProvider(CryptoBackendRoutingOptions options, IOpenSslBridgeClient bridgeClient)
    {
        _options = options;
        _bridgeClient = bridgeClient;
    }

    public CryptoBackendDiagnosticsSnapshot GetSnapshot()
    {
        var diagnostics = _bridgeClient.Diagnostics;
        return new CryptoBackendDiagnosticsSnapshot(
            true,
            diagnostics.IsAvailable,
            diagnostics.Version,
            GetCapabilities(diagnostics.Capabilities),
            _options.DefaultPreference,
            BuildRoutingSummary(diagnostics),
            diagnostics.LastLoadError);
    }

    private string BuildRoutingSummary(OpenSslDiagnosticsSnapshot diagnostics)
    {
        return _options.DefaultPreference switch
        {
            CryptoBackendPreference.PreferOpenSsl when diagnostics.IsAvailable
                => "OpenSSL is preferred when supported. Managed remains the fallback for unavailable operations.",
            CryptoBackendPreference.PreferOpenSsl
                => "OpenSSL is preferred by configuration, but managed fallback is active because the bridge is unavailable.",
            CryptoBackendPreference.OpenSslOnly when diagnostics.IsAvailable
                => "OpenSSL-only routing is configured for supported operations.",
            CryptoBackendPreference.OpenSslOnly
                => "OpenSSL-only routing is configured, but the bridge is currently unavailable.",
            _ when diagnostics.IsAvailable
                => "Managed remains the default backend. OpenSSL is available only when explicitly requested for supported operations.",
            _ => "Managed remains the default backend. OpenSSL is optional and currently unavailable."
        };
    }

    private static IReadOnlyList<string> GetCapabilities(OpenSslBridgeCapabilities capabilities)
    {
        var items = new List<string>();
        if (capabilities.HasFlag(OpenSslBridgeCapabilities.SupportsCertificateSigningRequestSigning))
        {
            items.Add("CSR signing");
        }

        return items;
    }
}
