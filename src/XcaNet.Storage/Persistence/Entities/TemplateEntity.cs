namespace XcaNet.Storage.Persistence.Entities;

public sealed class TemplateEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsDisabled { get; set; }
}
