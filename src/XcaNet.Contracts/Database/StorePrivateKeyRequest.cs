namespace XcaNet.Contracts.Database;

public sealed record StorePrivateKeyRequest(
    string DisplayName,
    string Algorithm,
    string PublicKeyFingerprint,
    byte[] Pkcs8Bytes,
    string Source);
