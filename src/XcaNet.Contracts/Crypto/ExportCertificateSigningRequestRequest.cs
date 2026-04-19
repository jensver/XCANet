namespace XcaNet.Contracts.Crypto;

public sealed record ExportCertificateSigningRequestRequest(
    byte[] CertificateSigningRequestDer,
    CryptoDataFormat Format,
    string FileNameStem);
