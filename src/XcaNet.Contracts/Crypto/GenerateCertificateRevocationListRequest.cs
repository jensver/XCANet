using XcaNet.Contracts.Revocation;

namespace XcaNet.Contracts.Crypto;

public sealed record GenerateCertificateRevocationListRequest(
    byte[] IssuerCertificateDer,
    byte[] IssuerPrivateKeyPkcs8,
    string IssuerPrivateKeyAlgorithm,
    long CrlNumber,
    DateTimeOffset ThisUpdate,
    DateTimeOffset NextUpdate,
    IReadOnlyList<RevokedCertificateEntry> RevokedCertificates);
