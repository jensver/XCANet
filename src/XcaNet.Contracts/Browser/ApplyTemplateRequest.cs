namespace XcaNet.Contracts.Browser;

public sealed record ApplyTemplateRequest(
    Guid TemplateId,
    TemplateWorkflowKind Workflow);
