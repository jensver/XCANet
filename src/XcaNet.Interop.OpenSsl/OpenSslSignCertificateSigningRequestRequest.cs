namespace XcaNet.Interop.OpenSsl;

public sealed record OpenSslSignCertificateSigningRequestRequest(
    byte[] CertificateSigningRequestDer,
    byte[] IssuerCertificateDer,
    byte[] IssuerPrivateKeyPkcs8,
    int ValidityDays);
