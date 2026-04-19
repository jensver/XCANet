namespace XcaNet.Contracts.Crypto;

public sealed record SignCertificateSigningRequestRequest(
    byte[] CertificateSigningRequestDer,
    byte[] IssuerCertificateDer,
    byte[] IssuerPrivateKeyPkcs8,
    string IssuerPrivateKeyAlgorithm,
    int ValidityDays,
    CryptoBackendPreference? PreferredBackend = null);
