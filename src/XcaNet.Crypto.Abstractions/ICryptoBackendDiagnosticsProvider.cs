using XcaNet.Contracts.Crypto;

namespace XcaNet.Crypto.Abstractions;

public interface ICryptoBackendDiagnosticsProvider
{
    CryptoBackendDiagnosticsSnapshot GetSnapshot();
}
