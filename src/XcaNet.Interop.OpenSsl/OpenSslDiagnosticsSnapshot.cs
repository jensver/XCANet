namespace XcaNet.Interop.OpenSsl;

public sealed record OpenSslDiagnosticsSnapshot(
    bool IsAvailable,
    string? Version,
    OpenSslBridgeCapabilities Capabilities,
    string? LastLoadError,
    string? ResolvedLibraryPath);
