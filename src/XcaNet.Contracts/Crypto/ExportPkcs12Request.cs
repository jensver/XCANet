namespace XcaNet.Contracts.Crypto;

public sealed record ExportPkcs12Request(
    byte[] CertificateDer,
    byte[] PrivateKeyPkcs8,
    string PrivateKeyAlgorithm,
    string FileNameStem,
    string Password);
