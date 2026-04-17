namespace XcaNet.Contracts.Crypto;

public sealed record ImportedCertificateMaterial(
    string DisplayName,
    byte[] DerData,
    CertificateDetails Details);
