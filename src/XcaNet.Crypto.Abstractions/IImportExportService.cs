using XcaNet.Contracts.Crypto;
using XcaNet.Contracts.Results;

namespace XcaNet.Crypto.Abstractions;

public interface IImportExportService
{
    Task<OperationResult<ImportCertificateMaterialResult>> ImportAsync(ImportCertificateMaterialRequest request, CancellationToken cancellationToken);

    Task<OperationResult<ExportedArtifact>> ExportCertificateAsync(ExportCertificateRequest request, CancellationToken cancellationToken);

    Task<OperationResult<ExportedArtifact>> ExportCertificateSigningRequestAsync(ExportCertificateSigningRequestRequest request, CancellationToken cancellationToken);

    Task<OperationResult<ExportedArtifact>> ExportPkcs12Async(ExportPkcs12Request request, CancellationToken cancellationToken);
}
