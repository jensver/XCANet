using XcaNet.Storage.Persistence.Entities;

namespace XcaNet.Storage.Repositories;

public interface ICertificateRepository
{
    Task AddAsync(string databasePath, CertificateEntity certificate, CancellationToken cancellationToken);

    Task<CertificateEntity?> GetAsync(string databasePath, Guid certificateId, CancellationToken cancellationToken);
}
