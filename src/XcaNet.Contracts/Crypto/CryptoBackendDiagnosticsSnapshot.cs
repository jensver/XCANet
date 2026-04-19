namespace XcaNet.Contracts.Crypto;

public sealed record CryptoBackendDiagnosticsSnapshot(
    bool ManagedBackendAvailable,
    bool OpenSslBackendAvailable,
    string? OpenSslVersion,
    IReadOnlyList<string> OpenSslCapabilities,
    CryptoBackendPreference DefaultPreference,
    string RoutingSummary,
    string? OpenSslLoadError);
