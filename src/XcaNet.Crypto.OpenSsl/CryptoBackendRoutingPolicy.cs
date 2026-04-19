using XcaNet.Contracts.Crypto;
using XcaNet.Interop.OpenSsl;

namespace XcaNet.Crypto.OpenSsl;

public sealed class CryptoBackendRoutingPolicy
{
    private readonly CryptoBackendRoutingOptions _options;
    private readonly OpenSslDiagnosticsSnapshot _diagnostics;

    public CryptoBackendRoutingPolicy(CryptoBackendRoutingOptions options, OpenSslDiagnosticsSnapshot diagnostics)
    {
        _options = options;
        _diagnostics = diagnostics;
    }

    public CryptoBackendRoutingDecision SelectCertificateSigningBackend(CryptoBackendPreference? requestedPreference)
    {
        var preference = requestedPreference ?? _options.DefaultPreference;
        if (preference == CryptoBackendPreference.PreferManaged)
        {
            return new CryptoBackendRoutingDecision(preference, CryptoBackendKind.Managed, false, "Managed backend is preferred.");
        }

        if (_diagnostics.IsAvailable && _diagnostics.Capabilities.HasFlag(OpenSslBridgeCapabilities.SupportsCertificateSigningRequestSigning))
        {
            return new CryptoBackendRoutingDecision(preference, CryptoBackendKind.OpenSsl, false, "OpenSSL backend is available for CSR signing.");
        }

        return preference == CryptoBackendPreference.OpenSslOnly
            ? new CryptoBackendRoutingDecision(preference, CryptoBackendKind.OpenSsl, false, _diagnostics.LastLoadError ?? "OpenSSL backend is unavailable.")
            : new CryptoBackendRoutingDecision(preference, CryptoBackendKind.Managed, true, _diagnostics.LastLoadError ?? "OpenSSL backend is unavailable; falling back to managed.");
    }
}
