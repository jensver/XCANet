namespace XcaNet.Contracts.Crypto;

public sealed record PrivateKeyImportResult(
    string DisplayName,
    string Algorithm,
    string PublicKeyFingerprint,
    byte[] Pkcs8PrivateKey,
    byte[] SubjectPublicKeyInfo);
