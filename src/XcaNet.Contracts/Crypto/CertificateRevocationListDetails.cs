using XcaNet.Contracts.Revocation;

namespace XcaNet.Contracts.Crypto;

public sealed record CertificateRevocationListDetails(
    string Issuer,
    string CrlNumber,
    DateTimeOffset ThisUpdate,
    DateTimeOffset? NextUpdate,
    IReadOnlyList<RevokedCertificateEntry> RevokedCertificates);
