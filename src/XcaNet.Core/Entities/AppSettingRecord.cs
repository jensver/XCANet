namespace XcaNet.Core.Entities;

public sealed record AppSettingRecord(
    string Key,
    string Value,
    DateTime UpdatedUtc);
