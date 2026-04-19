using XcaNet.Core.Enums;

namespace XcaNet.Core.Entities;

public sealed record CertificateRecord(
    Guid Id,
    string DisplayName,
    string Subject,
    string Issuer,
    string SerialNumber,
    string Sha1Thumbprint,
    string Sha256Thumbprint,
    DateTime? NotBeforeUtc,
    DateTime? NotAfterUtc,
    RevocationState RevocationState,
    Guid? IssuerCertificateId,
    Guid? PrivateKeyId);
