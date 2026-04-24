namespace XcaNet.Contracts.Browser;

public sealed record CertificateListItem(
    Guid CertificateId,
    string DisplayName,
    string Subject,
    string Issuer,
    string SerialNumber,
    string Sha1Thumbprint,
    string Sha256Thumbprint,
    DateTimeOffset? NotBefore,
    DateTimeOffset? NotAfter,
    string KeyAlgorithm,
    bool IsCertificateAuthority,
    string RevocationStatus,
    string? RevocationReason,
    DateTimeOffset? RevokedAt,
    Guid? IssuerCertificateId,
    Guid? PrivateKeyId,
    int ChildCertificateCount)
{
    public string CertificateKind => IsCertificateAuthority ? "CA" : "Leaf";

    public string PrivateKeyStatus => PrivateKeyId is null ? "No" : "Yes";
}
