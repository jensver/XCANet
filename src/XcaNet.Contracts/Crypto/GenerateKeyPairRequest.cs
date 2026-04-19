namespace XcaNet.Contracts.Crypto;

public sealed record GenerateKeyPairRequest(
    string DisplayName,
    KeyAlgorithmKind Algorithm,
    int? RsaKeySize,
    EllipticCurveKind? Curve);
