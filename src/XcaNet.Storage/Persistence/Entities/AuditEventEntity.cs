namespace XcaNet.Storage.Persistence.Entities;

public sealed class AuditEventEntity
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public DateTime OccurredUtc { get; set; }
}
