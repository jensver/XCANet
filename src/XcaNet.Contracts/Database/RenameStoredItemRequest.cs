using XcaNet.Contracts.Browser;

namespace XcaNet.Contracts.Database;

public sealed record RenameStoredItemRequest(BrowserEntityType Kind, Guid Id, string NewName);
