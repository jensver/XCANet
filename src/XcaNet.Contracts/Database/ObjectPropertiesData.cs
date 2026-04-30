using XcaNet.Contracts.Browser;

namespace XcaNet.Contracts.Database;

public sealed record ObjectPropertiesData(
    BrowserEntityType Kind,
    Guid Id,
    string Name,
    string? Comment,
    string Source,
    string? CreatedAt);
