using XcaNet.Contracts.Database;

namespace XcaNet.Contracts.Crypto.Workflow;

public sealed record ApplicationDiagnosticsSnapshot(
    CryptoBackendDiagnosticsSnapshot CryptoBackends,
    int SchemaVersion,
    string AppVersion,
    DatabaseSessionState SessionState);
