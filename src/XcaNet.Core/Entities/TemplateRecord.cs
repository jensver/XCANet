namespace XcaNet.Core.Entities;

public sealed record TemplateRecord(
    Guid Id,
    string Name,
    string? Description,
    bool IsFavorite,
    bool IsDisabled);
