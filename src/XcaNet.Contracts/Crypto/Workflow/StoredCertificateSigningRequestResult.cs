namespace XcaNet.Contracts.Crypto.Workflow;

public sealed record StoredCertificateSigningRequestResult(
    Guid CertificateSigningRequestId,
    Guid PrivateKeyId,
    CertificateSigningRequestDetails Details);
