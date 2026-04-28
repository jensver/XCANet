using XcaNet.Contracts.Database;
using XcaNet.Contracts.Browser;
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

    Task<OperationResult<DatabaseSessionSnapshot>> CloseDatabaseAsync(CancellationToken cancellationToken);

    Task<OperationResult<StorePrivateKeyResult>> StorePrivateKeyAsync(StorePrivateKeyRequest request, CancellationToken cancellationToken);

    Task<OperationResult<StoredKeyResult>> GenerateStoredKeyAsync(GenerateStoredKeyRequest request, CancellationToken cancellationToken);

    Task<OperationResult<StoredCertificateResult>> CreateSelfSignedCaAsync(CreateSelfSignedCaWorkflowRequest request, CancellationToken cancellationToken);

    Task<OperationResult<StoredCertificateSigningRequestResult>> CreateCertificateSigningRequestAsync(CreateCertificateSigningRequestWorkflowRequest request, CancellationToken cancellationToken);

    Task<OperationResult<StoredCertificateResult>> SignCertificateSigningRequestAsync(SignStoredCertificateSigningRequestRequest request, CancellationToken cancellationToken);

    Task<OperationResult<StoredCertificateResult>> RevokeCertificateAsync(RevokeStoredCertificateRequest request, CancellationToken cancellationToken);

    Task<OperationResult<StoredCertificateRevocationListResult>> GenerateCertificateRevocationListAsync(GenerateCertificateRevocationListWorkflowRequest request, CancellationToken cancellationToken);

    Task<OperationResult<ImportStoredMaterialResult>> ImportStoredMaterialAsync(ImportStoredMaterialRequest request, CancellationToken cancellationToken);

    Task<OperationResult<ImportStoredFilesResult>> ImportStoredFilesAsync(ImportStoredFilesRequest request, CancellationToken cancellationToken);

    Task<OperationResult<ExportedArtifact>> ExportStoredMaterialAsync(ExportStoredMaterialRequest request, CancellationToken cancellationToken);

    Task<OperationResult<ExportedArtifact>> ExportStoredMaterialToFileAsync(ExportStoredMaterialToFileRequest request, CancellationToken cancellationToken);

    Task<OperationResult<ApplicationDiagnosticsSnapshot>> GetApplicationDiagnosticsAsync(CancellationToken cancellationToken);

    Task<OperationResult<CertificateDetails>> GetCertificateDetailsAsync(Guid certificateId, CancellationToken cancellationToken);

    Task<OperationResult<DashboardSummary>> GetDashboardSummaryAsync(CancellationToken cancellationToken);

    Task<OperationResult<IReadOnlyList<CertificateListItem>>> ListCertificatesAsync(CertificateFilterState filter, CancellationToken cancellationToken);

    Task<OperationResult<CertificateInspectorData>> GetCertificateInspectorAsync(Guid certificateId, CancellationToken cancellationToken);

    Task<OperationResult<IReadOnlyList<PrivateKeyListItem>>> ListPrivateKeysAsync(CancellationToken cancellationToken);

    Task<OperationResult<IReadOnlyList<CertificateRequestListItem>>> ListCertificateSigningRequestsAsync(CancellationToken cancellationToken);

    Task<OperationResult<IReadOnlyList<CertificateRevocationListItem>>> ListCertificateRevocationListsAsync(CancellationToken cancellationToken);

    Task<OperationResult<CertificateRevocationListInspectorData>> GetCertificateRevocationListInspectorAsync(Guid certificateRevocationListId, CancellationToken cancellationToken);

    Task<OperationResult<IReadOnlyList<TemplateListItem>>> ListTemplatesAsync(CancellationToken cancellationToken);

    Task<OperationResult<TemplateDetails>> GetTemplateAsync(Guid templateId, CancellationToken cancellationToken);

    Task<OperationResult<TemplateDetails>> SaveTemplateAsync(SaveTemplateRequest request, CancellationToken cancellationToken);

    Task<OperationResult<TemplateDetails>> CloneTemplateAsync(CloneTemplateRequest request, CancellationToken cancellationToken);

    Task<OperationResult<TemplateDetails>> SetTemplateFavoriteAsync(SetTemplateFavoriteRequest request, CancellationToken cancellationToken);

    Task<OperationResult<TemplateDetails>> SetTemplateEnabledAsync(SetTemplateEnabledRequest request, CancellationToken cancellationToken);

    Task<OperationResult> DeleteTemplateAsync(Guid templateId, CancellationToken cancellationToken);

    Task<OperationResult<AppliedTemplateDefaults>> ApplyTemplateAsync(ApplyTemplateRequest request, CancellationToken cancellationToken);

    Task<OperationResult> ChangePasswordAsync(string newPassword, CancellationToken cancellationToken);

    Task<OperationResult> RenameStoredItemAsync(RenameStoredItemRequest request, CancellationToken cancellationToken);

    DatabaseSessionSnapshot GetSnapshot();
}
