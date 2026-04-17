namespace XcaNet.Contracts.Crypto.Workflow;

public sealed record CreateSelfSignedCaWorkflowRequest(
    Guid PrivateKeyId,
    string DisplayName,
    string SubjectName,
    int ValidityDays);
