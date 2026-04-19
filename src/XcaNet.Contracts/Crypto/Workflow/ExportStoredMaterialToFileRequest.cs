namespace XcaNet.Contracts.Crypto.Workflow;

public sealed record ExportStoredMaterialToFileRequest(
    CryptoImportKind Kind,
    Guid MaterialId,
    CryptoDataFormat Format,
    string DestinationPath,
    string? Password,
    string FileNameStem,
    StoredMaterialExportMode Mode = StoredMaterialExportMode.Default);
