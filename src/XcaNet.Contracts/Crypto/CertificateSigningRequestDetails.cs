namespace XcaNet.Contracts.Crypto;

public sealed record CertificateSigningRequestDetails(
    string Subject,
    string KeyAlgorithm,
    IReadOnlyList<string> SubjectAlternativeNames);
