namespace XcaNet.Contracts.Browser;

public sealed record RelatedNavigationItem(
    string DisplayName,
    string Description,
    NavigationTarget Target);
