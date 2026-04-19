namespace XcaNet.Contracts.Browser;

public sealed record CertificateRequestListItem(
    Guid CertificateSigningRequestId,
    string DisplayName,
    string Subject,
    Guid? PrivateKeyId,
    NavigationTarget? PrivateKeyTarget,
    string KeyAlgorithm,
    string SubjectAlternativeNames,
    DateTimeOffset CreatedUtc);
