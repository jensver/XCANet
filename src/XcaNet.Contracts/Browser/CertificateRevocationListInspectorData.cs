using XcaNet.Contracts.Revocation;

namespace XcaNet.Contracts.Browser;

public sealed record CertificateRevocationListInspectorData(
    Guid CertificateRevocationListId,
    string DisplayName,
    string IssuerDisplayName,
    NavigationTarget IssuerTarget,
    string CrlNumber,
    DateTimeOffset ThisUpdate,
    DateTimeOffset? NextUpdate,
    IReadOnlyList<RevokedCertificateEntry> RevokedEntries);
