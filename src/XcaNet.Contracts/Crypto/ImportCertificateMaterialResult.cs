namespace XcaNet.Contracts.Crypto;

public sealed record ImportCertificateMaterialResult(
    IReadOnlyList<ImportedPrivateKeyMaterial> PrivateKeys,
    IReadOnlyList<ImportedCertificateMaterial> Certificates,
    IReadOnlyList<ImportedCertificateSigningRequestMaterial> CertificateSigningRequests);
