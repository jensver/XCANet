namespace XcaNet.Storage.Persistence.Entities;

public sealed class CertificateEntity
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Sha1Thumbprint { get; set; } = string.Empty;
    public string Sha256Thumbprint { get; set; } = string.Empty;
    public DateTime? NotBeforeUtc { get; set; }
    public DateTime? NotAfterUtc { get; set; }
    public int RevocationState { get; set; }
    public Guid? IssuerCertificateId { get; set; }
    public Guid? PrivateKeyId { get; set; }
    public string? PemData { get; set; }
    public ICollection<CertificateTagEntity> CertificateTags { get; set; } = [];
}
