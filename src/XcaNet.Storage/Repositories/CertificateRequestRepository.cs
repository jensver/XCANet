using Microsoft.EntityFrameworkCore;
using XcaNet.Storage.Persistence;
using XcaNet.Storage.Persistence.Entities;

namespace XcaNet.Storage.Repositories;

public sealed class CertificateRequestRepository : ICertificateRequestRepository
{
    private readonly IXcaNetDbContextFactory _dbContextFactory;

    public CertificateRequestRepository(IXcaNetDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task AddAsync(string databasePath, CertificateRequestEntity certificateRequest, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        dbContext.CertificateRequests.Add(certificateRequest);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<CertificateRequestEntity?> GetAsync(string databasePath, Guid certificateRequestId, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        return await dbContext.CertificateRequests.SingleOrDefaultAsync(x => x.Id == certificateRequestId, cancellationToken);
    }
}
