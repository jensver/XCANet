namespace XcaNet.Contracts.Crypto.Workflow;

public sealed record ImportStoredMaterialRequest(
    string DisplayName,
    CryptoImportKind Kind,
    CryptoDataFormat Format,
    byte[] Data,
    string? Password);
