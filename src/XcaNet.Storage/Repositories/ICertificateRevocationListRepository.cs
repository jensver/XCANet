using XcaNet.Storage.Persistence.Entities;

namespace XcaNet.Storage.Repositories;

public interface ICertificateRevocationListRepository
{
    Task<IReadOnlyList<CertificateRevocationListEntity>> ListAsync(string databasePath, CancellationToken cancellationToken);
}
