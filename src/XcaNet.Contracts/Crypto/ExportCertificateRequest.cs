namespace XcaNet.Contracts.Crypto;

public sealed record ExportCertificateRequest(
    byte[] CertificateDer,
    CryptoDataFormat Format,
    string FileNameStem);
