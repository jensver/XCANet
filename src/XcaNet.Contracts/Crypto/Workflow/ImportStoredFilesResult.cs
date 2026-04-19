namespace XcaNet.Contracts.Crypto.Workflow;

public sealed record ImportStoredFilesResult(
    IReadOnlyList<ImportedStoredFileItem> ImportedFiles);
