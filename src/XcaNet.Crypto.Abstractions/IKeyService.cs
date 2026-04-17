using XcaNet.Contracts.Crypto;
using XcaNet.Contracts.Results;

namespace XcaNet.Crypto.Abstractions;

public interface IKeyService
{
    Task<OperationResult<GenerateKeyPairResult>> GenerateAsync(GenerateKeyPairRequest request, CancellationToken cancellationToken);

    Task<OperationResult<PrivateKeyImportResult>> ImportPrivateKeyAsync(PrivateKeyImportRequest request, CancellationToken cancellationToken);

    Task<OperationResult<ExportedArtifact>> ExportPrivateKeyAsync(PrivateKeyExportRequest request, CancellationToken cancellationToken);
}
