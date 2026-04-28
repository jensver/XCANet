using XcaNet.Storage.Persistence.Entities;

namespace XcaNet.Storage.Repositories;

public interface ICertificateRevocationListRepository
{
    Task AddAsync(string databasePath, CertificateRevocationListEntity certificateRevocationList, CancellationToken cancellationToken);

    Task<CertificateRevocationListEntity?> GetAsync(string databasePath, Guid certificateRevocationListId, CancellationToken cancellationToken);

    Task<IReadOnlyList<CertificateRevocationListEntity>> ListAsync(string databasePath, CancellationToken cancellationToken);

    Task UpdateDisplayNameAsync(string databasePath, Guid certificateRevocationListId, string newName, CancellationToken cancellationToken);
}
