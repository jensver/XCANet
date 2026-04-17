namespace XcaNet.Contracts.Crypto.Workflow;

public sealed record StoredCertificateResult(
    Guid CertificateId,
    Guid? PrivateKeyId,
    CertificateDetails Details);
