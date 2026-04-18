namespace XcaNet.Contracts.Browser;

public sealed record NavigationTarget(
    BrowserEntityType EntityType,
    Guid EntityId,
    NavigationFocusSection? FocusSection);
