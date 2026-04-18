namespace XcaNet.Contracts.Browser;

public sealed record CertificateRevocationListItem(
    Guid CertificateRevocationListId,
    string DisplayName,
    Guid IssuerCertificateId,
    string IssuerDisplayName,
    string CrlNumber,
    DateTimeOffset ThisUpdate,
    DateTimeOffset? NextUpdateUtc,
    int RevokedEntryCount,
    NavigationTarget IssuerTarget);
