using XcaNet.Storage.Persistence.Entities;

namespace XcaNet.Storage.Repositories;

public interface IPrivateKeyRepository
{
    Task AddAsync(string databasePath, PrivateKeyEntity privateKey, CancellationToken cancellationToken);

    Task<PrivateKeyEntity?> GetAsync(string databasePath, Guid privateKeyId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PrivateKeyEntity>> ListAsync(string databasePath, CancellationToken cancellationToken);

    Task UpdateDisplayNameAsync(string databasePath, Guid privateKeyId, string newName, CancellationToken cancellationToken);

    Task UpdateCommentAsync(string databasePath, Guid privateKeyId, string? comment, CancellationToken cancellationToken);
}
