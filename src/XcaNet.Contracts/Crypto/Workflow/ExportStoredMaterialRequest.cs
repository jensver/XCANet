namespace XcaNet.Contracts.Crypto.Workflow;

public sealed record ExportStoredMaterialRequest(
    CryptoImportKind Kind,
    Guid MaterialId,
    CryptoDataFormat Format,
    string? Password,
    string FileNameStem);
