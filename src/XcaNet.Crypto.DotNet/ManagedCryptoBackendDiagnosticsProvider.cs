using XcaNet.Contracts.Crypto;
using XcaNet.Crypto.Abstractions;

namespace XcaNet.Crypto.DotNet;

public sealed class ManagedCryptoBackendDiagnosticsProvider : ICryptoBackendDiagnosticsProvider
{
    public CryptoBackendDiagnosticsSnapshot GetSnapshot()
    {
        return new CryptoBackendDiagnosticsSnapshot(
            true,
            false,
            null,
            [],
            CryptoBackendPreference.PreferManaged,
            "Managed backend is the active default. OpenSSL routing is not configured in this service collection.",
            null);
    }
}
