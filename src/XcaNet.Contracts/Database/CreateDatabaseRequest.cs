namespace XcaNet.Contracts.Database;

public sealed record CreateDatabaseRequest(
    string DatabasePath,
    string Password,
    string DisplayName);
