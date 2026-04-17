namespace XcaNet.Contracts.Browser;

public sealed record CertificateBrowserQuery(
    string? SearchText,
    CertificateValidityFilter ValidityFilter,
    CertificateAuthorityFilter AuthorityFilter,
    int ExpiringSoonWithinDays);
