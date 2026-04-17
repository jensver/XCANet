namespace XcaNet.Contracts.Crypto;

public sealed record PrivateKeyExportRequest(
    string DisplayName,
    string Algorithm,
    byte[] Pkcs8PrivateKey,
    CryptoDataFormat Format,
    string? Password);
