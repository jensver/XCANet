using XcaNet.Contracts.Crypto;

namespace XcaNet.Contracts.Browser;

public sealed record AppliedTemplateDefaults(
    Guid TemplateId,
    string DisplayNameDefault,
    TemplateWorkflowKind Workflow,
    string? SubjectDefault,
    IReadOnlyList<string> SubjectAlternativeNames,
    KeyAlgorithmKind KeyAlgorithm,
    int? RsaKeySize,
    EllipticCurveKind? Curve,
    string SignatureAlgorithm,
    int ValidityDays,
    bool IsCertificateAuthority,
    int? PathLengthConstraint,
    IReadOnlyList<string> KeyUsages,
    IReadOnlyList<string> EnhancedKeyUsages,
    TemplatePreviewSummary Preview,
    TemplateValidationSummary Validation);
