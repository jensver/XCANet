namespace XcaNet.Contracts.Crypto;

public sealed record SelfSignedCaCertificateRequest(
    string SubjectName,
    byte[] Pkcs8PrivateKey,
    string KeyAlgorithm,
    int ValidityDays,
    IReadOnlyList<SanEntry>? SubjectAlternativeNames = null,
    bool IsCertificateAuthority = true,
    int? PathLengthConstraint = null,
    IReadOnlyList<string>? KeyUsages = null,
    IReadOnlyList<string>? EnhancedKeyUsages = null);
