namespace XcaNet.Contracts.Browser;

public sealed record DashboardSummary(
    int Certificates,
    int PrivateKeys,
    int CertificateSigningRequests,
    int CertificateRevocationLists,
    int Templates);
