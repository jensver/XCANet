using XcaNet.Contracts.Revocation;

namespace XcaNet.Contracts.Crypto.Workflow;

public sealed record RevokeStoredCertificateRequest(
    Guid CertificateId,
    CertificateRevocationReason Reason,
    DateTimeOffset RevokedAt);
