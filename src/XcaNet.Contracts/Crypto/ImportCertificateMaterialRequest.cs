namespace XcaNet.Contracts.Crypto;

public sealed record ImportCertificateMaterialRequest(
    CryptoImportKind Kind,
    CryptoDataFormat Format,
    byte[] Data,
    string? Password,
    string DisplayName);
