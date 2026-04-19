namespace XcaNet.Storage.Persistence.Entities;

public sealed class CertificateRevocationListEntity
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public Guid IssuerCertificateId { get; set; }
    public string IssuerDisplayName { get; set; } = string.Empty;
    public long CrlNumber { get; set; }
    public DateTime ThisUpdateUtc { get; set; }
    public DateTime? NextUpdateUtc { get; set; }
    public byte[] DerData { get; set; } = [];
    public string? PemData { get; set; }
    public ICollection<CertificateRevocationListEntryEntity> RevokedEntries { get; set; } = [];
}
