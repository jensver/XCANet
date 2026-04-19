using XcaNet.Contracts.Revocation;

namespace XcaNet.Contracts.Browser;

public sealed record CertificateRevocationInfo(
    bool IsRevoked,
    string Status,
    CertificateRevocationReason? Reason,
    DateTimeOffset? RevokedAt,
    string? ReasonDisplayName);
