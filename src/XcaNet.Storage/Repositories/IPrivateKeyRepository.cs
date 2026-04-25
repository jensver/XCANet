using XcaNet.Storage.Persistence.Entities;

namespace XcaNet.Storage.Repositories;

public interface IPrivateKeyRepository
{
    Task AddAsync(string databasePath, PrivateKeyEntity privateKey, CancellationToken cancellationToken);

    Task<PrivateKeyEntity?> GetAsync(string databasePath, Guid privateKeyId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PrivateKeyEntity>> ListAsync(string databasePath, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(string databasePath, Guid privateKeyId, CancellationToken cancellationToken);
}
