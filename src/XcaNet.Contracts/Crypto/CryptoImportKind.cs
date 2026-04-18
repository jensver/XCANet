namespace XcaNet.Contracts.Crypto;

public enum CryptoImportKind
{
    Certificate = 0,
    PrivateKey = 1,
    CertificateSigningRequest = 2,
    Bundle = 3,
    CertificateRevocationList = 4
}
