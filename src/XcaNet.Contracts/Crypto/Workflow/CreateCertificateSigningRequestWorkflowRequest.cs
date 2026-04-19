namespace XcaNet.Contracts.Crypto.Workflow;

public sealed record CreateCertificateSigningRequestWorkflowRequest(
    Guid PrivateKeyId,
    string DisplayName,
    string SubjectName,
    IReadOnlyList<SanEntry> SubjectAlternativeNames,
    bool IsCertificateAuthority = false,
    int? PathLengthConstraint = null,
    IReadOnlyList<string>? KeyUsages = null,
    IReadOnlyList<string>? EnhancedKeyUsages = null,
    Guid? TemplateId = null);
