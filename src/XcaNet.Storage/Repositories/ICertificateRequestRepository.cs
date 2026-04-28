using XcaNet.Storage.Persistence.Entities;

namespace XcaNet.Storage.Repositories;

public interface ICertificateRequestRepository
{
    Task AddAsync(string databasePath, CertificateRequestEntity certificateRequest, CancellationToken cancellationToken);

    Task<CertificateRequestEntity?> GetAsync(string databasePath, Guid certificateRequestId, CancellationToken cancellationToken);

    Task<IReadOnlyList<CertificateRequestEntity>> ListAsync(string databasePath, CancellationToken cancellationToken);

    Task UpdateDisplayNameAsync(string databasePath, Guid certificateRequestId, string newName, CancellationToken cancellationToken);
}
