namespace XcaNet.Contracts.Crypto.Workflow;

public sealed record CreateCertificateSigningRequestWorkflowRequest(
    Guid PrivateKeyId,
    string DisplayName,
    string SubjectName,
    IReadOnlyList<SanEntry> SubjectAlternativeNames);
