namespace XcaNet.Storage.Persistence.Entities;

public sealed class CertificateTagEntity
{
    public Guid CertificateId { get; set; }
    public CertificateEntity Certificate { get; set; } = null!;
    public Guid TagId { get; set; }
    public TagEntity Tag { get; set; } = null!;
}
