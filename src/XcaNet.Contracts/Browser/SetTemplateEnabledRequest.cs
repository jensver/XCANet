namespace XcaNet.Contracts.Browser;

public sealed record SetTemplateEnabledRequest(
    Guid TemplateId,
    bool IsEnabled);
