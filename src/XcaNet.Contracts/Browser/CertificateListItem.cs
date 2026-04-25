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

    public string CommonName => ExtractCommonName(Subject);

    public string StatusDisplay
    {
        get
        {
            if (string.Equals(RevocationStatus, "Revoked", StringComparison.OrdinalIgnoreCase))
                return "Revoked";
            var now = DateTimeOffset.UtcNow;
            if (NotBefore is { } nb && nb > now)
                return "Not yet valid";
            if (NotAfter is { } na && na < now)
                return "Expired";
            return "Valid";
        }
    }

    private static string ExtractCommonName(string subject)
    {
        foreach (var part in subject.Split(',', StringSplitOptions.TrimEntries))
        {
            if (part.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
                return part[3..];
        }
        return subject;
    }
}
