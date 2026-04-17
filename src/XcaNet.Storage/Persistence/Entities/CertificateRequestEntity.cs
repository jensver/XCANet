namespace XcaNet.Storage.Persistence.Entities;

public sealed class CertificateRequestEntity
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? PemData { get; set; }
    public DateTime CreatedUtc { get; set; }
}
