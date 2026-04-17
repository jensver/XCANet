namespace XcaNet.Storage.Persistence.Entities;

public sealed class AppSettingEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime UpdatedUtc { get; set; }
}
