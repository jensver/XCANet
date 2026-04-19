namespace XcaNet.Contracts.Browser;

public sealed record CloneTemplateRequest(
    Guid TemplateId,
    string? NewName);
