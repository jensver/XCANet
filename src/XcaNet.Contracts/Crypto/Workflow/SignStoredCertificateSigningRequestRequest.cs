namespace XcaNet.Contracts.Crypto.Workflow;

public sealed record SignStoredCertificateSigningRequestRequest(
    Guid CertificateSigningRequestId,
    Guid IssuerCertificateId,
    Guid IssuerPrivateKeyId,
    string DisplayName,
    int ValidityDays,
    Guid? TemplateId = null);
