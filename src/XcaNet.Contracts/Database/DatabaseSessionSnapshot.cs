namespace XcaNet.Contracts.Database;

public sealed record DatabaseSessionSnapshot(
    string? DatabasePath,
    string? DisplayName,
    DatabaseSessionState State,
    int SchemaVersion,
    DateTime? LastOpenedUtc,
    string StatusMessage);
