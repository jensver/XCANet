using XcaNet.Contracts.Database;
using XcaNet.Contracts.Results;

namespace XcaNet.Application.Services;

public interface IDatabaseSessionService
{
    Task<OperationResult<DatabaseSessionSnapshot>> CreateDatabaseAsync(CreateDatabaseRequest request, CancellationToken cancellationToken);

    Task<OperationResult<DatabaseSessionSnapshot>> OpenDatabaseAsync(OpenDatabaseRequest request, CancellationToken cancellationToken);

    Task<OperationResult<DatabaseSessionSnapshot>> UnlockDatabaseAsync(UnlockDatabaseRequest request, CancellationToken cancellationToken);

    Task<OperationResult<DatabaseSessionSnapshot>> LockDatabaseAsync(CancellationToken cancellationToken);

    Task<OperationResult<StorePrivateKeyResult>> StorePrivateKeyAsync(StorePrivateKeyRequest request, CancellationToken cancellationToken);

    DatabaseSessionSnapshot GetSnapshot();
}
