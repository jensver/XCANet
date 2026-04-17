using XcaNet.Storage.Persistence.Entities;

namespace XcaNet.Storage.Repositories;

public interface IDatabaseProfileRepository
{
    Task<bool> ExistsAsync(string databasePath, CancellationToken cancellationToken);

    Task<DatabaseProfileEntity?> GetAsync(string databasePath, CancellationToken cancellationToken);

    Task AddAsync(string databasePath, DatabaseProfileEntity profile, CancellationToken cancellationToken);

    Task UpdateLastOpenedUtcAsync(string databasePath, Guid profileId, DateTime openedUtc, CancellationToken cancellationToken);
}
