namespace XcaNet.Contracts.Crypto;

public sealed record GenerateKeyPairResult(
    string DisplayName,
    string Algorithm,
    string PublicKeyFingerprint,
    byte[] Pkcs8PrivateKey,
    byte[] SubjectPublicKeyInfo);
