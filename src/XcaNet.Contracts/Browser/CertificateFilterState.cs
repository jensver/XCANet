namespace XcaNet.Contracts.Browser;

public sealed record CertificateFilterState(
    string? DisplayName,
    string? Subject,
    string? Issuer,
    string? SerialNumber,
    string? Thumbprint,
    CertificateValidityFilter ValidityFilter,
    CertificateAuthorityFilter AuthorityFilter,
    int ExpiringSoonWithinDays);
