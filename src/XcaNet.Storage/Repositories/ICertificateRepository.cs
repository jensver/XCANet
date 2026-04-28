using XcaNet.Contracts.Browser;
using XcaNet.Storage.Persistence.Entities;

namespace XcaNet.Storage.Repositories;

public interface ICertificateRepository
{
    Task AddAsync(string databasePath, CertificateEntity certificate, CancellationToken cancellationToken);

    Task<CertificateEntity?> GetAsync(string databasePath, Guid certificateId, CancellationToken cancellationToken);

    Task<IReadOnlyList<CertificateEntity>> ListAsync(string databasePath, CancellationToken cancellationToken);

    Task<IReadOnlyList<CertificateEntity>> ListAsync(string databasePath, CertificateFilterState filter, CancellationToken cancellationToken);

    Task UpdateRevocationAsync(
        string databasePath,
        Guid certificateId,
        int revocationState,
        int? revocationReason,
        DateTime? revokedAtUtc,
        CancellationToken cancellationToken);

    Task UpdateDisplayNameAsync(string databasePath, Guid certificateId, string newName, CancellationToken cancellationToken);
}
