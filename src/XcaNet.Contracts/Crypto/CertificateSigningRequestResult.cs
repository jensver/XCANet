namespace XcaNet.Contracts.Crypto;

public sealed record CertificateSigningRequestResult(
    byte[] DerData,
    CertificateSigningRequestDetails Details);
