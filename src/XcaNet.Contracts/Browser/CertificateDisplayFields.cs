namespace XcaNet.Contracts.Browser;

public sealed record CertificateDisplayFields(
    string DisplayName,
    string ValidityRange,
    string CertificateKind,
    string IssuerDisplayName,
    string? PrivateKeyDisplayName);
