using XcaNet.Contracts.Database;
using XcaNet.Contracts.Crypto;
using XcaNet.Contracts.Crypto.Workflow;
using XcaNet.Contracts.Results;

namespace XcaNet.Application.Services;

public interface IDatabaseSessionService
{
    Task<OperationResult<DatabaseSessionSnapshot>> CreateDatabaseAsync(CreateDatabaseRequest request, CancellationToken cancellationToken);

    Task<OperationResult<DatabaseSessionSnapshot>> OpenDatabaseAsync(OpenDatabaseRequest request, CancellationToken cancellationToken);

    Task<OperationResult<DatabaseSessionSnapshot>> UnlockDatabaseAsync(UnlockDatabaseRequest request, CancellationToken cancellationToken);

    Task<OperationResult<DatabaseSessionSnapshot>> LockDatabaseAsync(CancellationToken cancellationToken);

    Task<OperationResult<StorePrivateKeyResult>> StorePrivateKeyAsync(StorePrivateKeyRequest request, CancellationToken cancellationToken);

    Task<OperationResult<StoredKeyResult>> GenerateStoredKeyAsync(GenerateStoredKeyRequest request, CancellationToken cancellationToken);

    Task<OperationResult<StoredCertificateResult>> CreateSelfSignedCaAsync(CreateSelfSignedCaWorkflowRequest request, CancellationToken cancellationToken);

    Task<OperationResult<StoredCertificateSigningRequestResult>> CreateCertificateSigningRequestAsync(CreateCertificateSigningRequestWorkflowRequest request, CancellationToken cancellationToken);

    Task<OperationResult<StoredCertificateResult>> SignCertificateSigningRequestAsync(SignStoredCertificateSigningRequestRequest request, CancellationToken cancellationToken);

    Task<OperationResult<ImportStoredMaterialResult>> ImportStoredMaterialAsync(ImportStoredMaterialRequest request, CancellationToken cancellationToken);

    Task<OperationResult<ExportedArtifact>> ExportStoredMaterialAsync(ExportStoredMaterialRequest request, CancellationToken cancellationToken);

    Task<OperationResult<CertificateDetails>> GetCertificateDetailsAsync(Guid certificateId, CancellationToken cancellationToken);

    DatabaseSessionSnapshot GetSnapshot();
}
