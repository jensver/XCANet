namespace XcaNet.Contracts.Browser;

public sealed record CertificateRevocationListItem(
    Guid CertificateRevocationListId,
    string DisplayName,
    Guid AuthorityId,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? NextUpdateUtc);
