namespace XcaNet.Core.Entities;

public sealed record CertificateRequestRecord(
    Guid Id,
    string DisplayName,
    string Subject,
    string? PemData,
    DateTime CreatedUtc);
