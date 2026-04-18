namespace XcaNet.Contracts.Crypto;

public sealed record ImportedCertificateRevocationListMaterial(
    string DisplayName,
    byte[] DerData,
    string? PemData,
    CertificateRevocationListDetails Details);
