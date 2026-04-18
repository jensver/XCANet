namespace XcaNet.Contracts.Crypto;

public sealed record CreateCertificateSigningRequestRequest(
    string SubjectName,
    byte[] Pkcs8PrivateKey,
    string KeyAlgorithm,
    IReadOnlyList<SanEntry> SubjectAlternativeNames,
    bool IsCertificateAuthority = false,
    int? PathLengthConstraint = null,
    IReadOnlyList<string>? KeyUsages = null,
    IReadOnlyList<string>? EnhancedKeyUsages = null);
