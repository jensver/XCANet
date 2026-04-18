namespace XcaNet.Contracts.Crypto.Workflow;

public sealed record GenerateCertificateRevocationListWorkflowRequest(
    Guid IssuerCertificateId,
    Guid IssuerPrivateKeyId,
    string DisplayName,
    int NextUpdateDays);
