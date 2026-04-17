using XcaNet.Contracts.Crypto;

namespace XcaNet.Contracts.Browser;

public sealed record CertificateInspector(
    Guid CertificateId,
    string DisplayName,
    CertificateDetails Details,
    string RevocationStatus,
    Guid? IssuerCertificateId,
    string? IssuerDisplayName,
    Guid? PrivateKeyId,
    string? PrivateKeyDisplayName,
    IReadOnlyList<RelatedCertificateSummary> ChildCertificates);
