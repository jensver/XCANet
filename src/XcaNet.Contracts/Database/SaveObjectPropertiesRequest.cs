using XcaNet.Contracts.Browser;

namespace XcaNet.Contracts.Database;

public sealed record SaveObjectPropertiesRequest(
    BrowserEntityType Kind,
    Guid Id,
    string Name,
    string? Comment);
