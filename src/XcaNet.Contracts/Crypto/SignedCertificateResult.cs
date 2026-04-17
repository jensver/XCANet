namespace XcaNet.Contracts.Crypto;

public sealed record SignedCertificateResult(
    byte[] DerData,
    CertificateDetails Details);
