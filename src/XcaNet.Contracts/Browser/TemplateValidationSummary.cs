namespace XcaNet.Contracts.Browser;

public sealed record TemplateValidationSummary(
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);
