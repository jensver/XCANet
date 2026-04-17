namespace XcaNet.Contracts.Crypto;

public sealed record CertificateDetails(
    string Subject,
    string Issuer,
    string SerialNumber,
    DateTimeOffset NotBefore,
    DateTimeOffset NotAfter,
    string Sha1Thumbprint,
    string Sha256Thumbprint,
    string KeyAlgorithm,
    bool IsCertificateAuthority,
    IReadOnlyList<string> KeyUsages,
    IReadOnlyList<string> EnhancedKeyUsages,
    IReadOnlyList<string> SubjectAlternativeNames);
