namespace XcaNet.Contracts.Crypto;

public sealed record ImportedPrivateKeyMaterial(
    string DisplayName,
    string Algorithm,
    string PublicKeyFingerprint,
    byte[] Pkcs8PrivateKey);
