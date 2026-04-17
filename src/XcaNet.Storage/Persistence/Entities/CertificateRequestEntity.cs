namespace XcaNet.Storage.Persistence.Entities;

public sealed class CertificateRequestEntity
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public Guid PrivateKeyId { get; set; }
    public byte[] DerData { get; set; } = [];
    public string DataFormat { get; set; } = string.Empty;
    public string KeyAlgorithm { get; set; } = string.Empty;
    public string SubjectAlternativeNames { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}
