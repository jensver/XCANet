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

    public async Task<IReadOnlyList<CertificateRequestEntity>> ListAsync(string databasePath, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        return await dbContext.CertificateRequests
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(string databasePath, Guid certificateRequestId, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        var existing = await dbContext.CertificateRequests.SingleOrDefaultAsync(x => x.Id == certificateRequestId, cancellationToken);
        if (existing is null)
            return false;

        dbContext.CertificateRequests.Remove(existing);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> MarkSignedAsync(string databasePath, Guid certificateRequestId, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        var existing = await dbContext.CertificateRequests.SingleOrDefaultAsync(x => x.Id == certificateRequestId, cancellationToken);
        if (existing is null)
            return false;

        existing.IsSigned = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
