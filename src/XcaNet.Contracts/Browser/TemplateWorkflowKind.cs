namespace XcaNet.Contracts.Browser;

public enum TemplateWorkflowKind
{
    GenerateKey = 0,
    SelfSignedCa = 1,
    CertificateSigningRequest = 2,
    SignCertificateSigningRequest = 3
}
