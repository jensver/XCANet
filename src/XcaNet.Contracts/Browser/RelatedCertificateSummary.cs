namespace XcaNet.Contracts.Browser;

public sealed record RelatedCertificateSummary(
    Guid CertificateId,
    string DisplayName,
    string Subject);
