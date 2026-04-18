namespace XcaNet.Contracts.Crypto.Workflow;

public sealed record ImportedStoredFileItem(
    string FilePath,
    string DisplayName,
    CryptoImportKind Kind,
    IReadOnlyList<Guid> PrivateKeyIds,
    IReadOnlyList<Guid> CertificateIds,
    IReadOnlyList<Guid> CertificateSigningRequestIds,
    IReadOnlyList<Guid> CertificateRevocationListIds);
