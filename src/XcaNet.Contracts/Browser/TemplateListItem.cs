namespace XcaNet.Contracts.Browser;

public sealed record TemplateListItem(
    Guid TemplateId,
    string Name,
    string? Description,
    TemplateIntendedUsage IntendedUsage,
    bool IsFavorite,
    bool IsEnabled,
    string Summary)
{
    public string EnabledState => IsEnabled ? "Enabled" : "Disabled";

    public string FavoriteState => IsFavorite ? "Favorite" : "Standard";
}
