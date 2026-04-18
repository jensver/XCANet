namespace XcaNet.Contracts.Browser;

public sealed record CertificateNavigationInfo(
    NavigationTarget? Issuer,
    NavigationTarget? PrivateKey,
    IReadOnlyList<RelatedNavigationItem> Children);
