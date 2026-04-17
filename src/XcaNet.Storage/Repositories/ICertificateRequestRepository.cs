using XcaNet.Storage.Persistence.Entities;

namespace XcaNet.Storage.Repositories;

public interface ICertificateRequestRepository
{
    Task AddAsync(string databasePath, CertificateRequestEntity certificateRequest, CancellationToken cancellationToken);

    Task<CertificateRequestEntity?> GetAsync(string databasePath, Guid certificateRequestId, CancellationToken cancellationToken);
}
