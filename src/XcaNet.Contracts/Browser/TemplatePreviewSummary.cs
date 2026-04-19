namespace XcaNet.Contracts.Browser;

public sealed record TemplatePreviewSummary(
    string UsageSummary,
    string SubjectSummary,
    string SanSummary,
    string KeySummary,
    string ValiditySummary,
    string ExtensionSummary,
    string StateSummary);
