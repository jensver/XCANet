namespace XcaNet.Contracts.Crypto;

public sealed record CertificateRevocationListResult(
    byte[] DerData,
    string PemData,
    CertificateRevocationListDetails Details);
