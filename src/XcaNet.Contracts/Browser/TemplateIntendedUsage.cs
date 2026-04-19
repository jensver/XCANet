namespace XcaNet.Contracts.Browser;

public enum TemplateIntendedUsage
{
    SelfSignedCa = 0,
    IntermediateCa = 1,
    EndEntityCertificate = 2,
    CertificateSigningRequest = 3
}
