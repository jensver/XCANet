using XcaNet.Contracts.Crypto;

namespace XcaNet.Contracts.Crypto.Workflow;

public sealed record StoredCertificateRevocationListResult(
    Guid CertificateRevocationListId,
    CertificateRevocationListDetails Details);
