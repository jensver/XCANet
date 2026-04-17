namespace XcaNet.Storage.Persistence.Entities;

public sealed class CertificateRevocationListEntity
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public Guid AuthorityId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? NextUpdateUtc { get; set; }
    public string? PemData { get; set; }
}
