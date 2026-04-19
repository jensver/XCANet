namespace XcaNet.Storage.Persistence.Entities;

public sealed class CertificateRevocationListEntryEntity
{
    public string SerialNumber { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public int Reason { get; set; }
    public DateTime RevokedAtUtc { get; set; }
}
