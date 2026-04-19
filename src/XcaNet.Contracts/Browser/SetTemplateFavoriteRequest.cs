namespace XcaNet.Contracts.Browser;

public sealed record SetTemplateFavoriteRequest(
    Guid TemplateId,
    bool IsFavorite);
