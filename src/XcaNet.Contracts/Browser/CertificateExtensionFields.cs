namespace XcaNet.Contracts.Browser;

public sealed record CertificateExtensionFields(
    bool IsCertificateAuthority,
    IReadOnlyList<string> SubjectAlternativeNames,
    IReadOnlyList<string> KeyUsages,
    IReadOnlyList<string> EnhancedKeyUsages);
