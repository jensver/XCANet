namespace XcaNet.Contracts.Browser;

public sealed record TemplateListItem(
    Guid TemplateId,
    string Name,
    string? Description,
    bool IsFavorite,
    bool IsDisabled);
