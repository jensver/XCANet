namespace XcaNet.Contracts.Crypto.Workflow;

public sealed record ImportStoredFilesRequest(
    IReadOnlyList<string> FilePaths,
    string? Password);
