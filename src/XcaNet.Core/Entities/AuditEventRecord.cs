namespace XcaNet.Core.Entities;

public sealed record AuditEventRecord(
    Guid Id,
    string EventType,
    string Message,
    DateTime OccurredUtc,
    string? MetadataJson);
