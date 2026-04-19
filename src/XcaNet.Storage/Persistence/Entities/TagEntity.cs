namespace XcaNet.Storage.Persistence.Entities;

public sealed class TagEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<CertificateTagEntity> CertificateTags { get; set; } = [];
}
