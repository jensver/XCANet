namespace XcaNet.Contracts.Crypto.Workflow;

public sealed record ImportStoredMaterialResult(
    IReadOnlyList<Guid> PrivateKeyIds,
    IReadOnlyList<Guid> CertificateIds,
    IReadOnlyList<Guid> CertificateSigningRequestIds);
