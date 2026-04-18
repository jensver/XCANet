namespace XcaNet.Contracts.Crypto;

public sealed record CertificateSigningRequestDetails(
    string Subject,
    string KeyAlgorithm,
    IReadOnlyList<string> SubjectAlternativeNames,
    bool IsCertificateAuthority = false,
    IReadOnlyList<string>? KeyUsages = null,
    IReadOnlyList<string>? EnhancedKeyUsages = null);
