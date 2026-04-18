namespace XcaNet.Contracts.Revocation;

public sealed record RevokedCertificateEntry(
    Guid CertificateId,
    string DisplayName,
    string Subject,
    string SerialNumber,
    CertificateRevocationReason Reason,
    DateTimeOffset RevokedAt);
