namespace XcaNet.Contracts.Crypto.Workflow;

public sealed record GenerateStoredKeyRequest(
    string DisplayName,
    KeyAlgorithmKind Algorithm,
    int? RsaKeySize,
    EllipticCurveKind? Curve);
