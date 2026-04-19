namespace XcaNet.Core.Entities;

public sealed record CertificateRevocationListRecord(
    Guid Id,
    string DisplayName,
    Guid AuthorityId,
    DateTime CreatedUtc,
    DateTime? NextUpdateUtc);
