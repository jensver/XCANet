namespace XcaNet.Contracts.Crypto;

public sealed record PrivateKeyImportRequest(
    byte[] Data,
    CryptoDataFormat Format,
    string DisplayName,
    string? Password);
