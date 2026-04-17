namespace XcaNet.Contracts.Crypto;

public sealed record ImportedCertificateSigningRequestMaterial(
    string DisplayName,
    byte[] DerData,
    CertificateSigningRequestDetails Details);
