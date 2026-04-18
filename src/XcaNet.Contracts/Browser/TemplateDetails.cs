using XcaNet.Contracts.Crypto;

namespace XcaNet.Contracts.Browser;

public sealed record TemplateDetails(
    Guid TemplateId,
    string Name,
    string? Description,
    bool IsFavorite,
    bool IsEnabled,
    TemplateIntendedUsage IntendedUsage,
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
