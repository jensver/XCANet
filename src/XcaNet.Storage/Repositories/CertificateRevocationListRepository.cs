using Microsoft.EntityFrameworkCore;
using XcaNet.Storage.Persistence;
using XcaNet.Storage.Persistence.Entities;

namespace XcaNet.Storage.Repositories;

public sealed class CertificateRevocationListRepository : ICertificateRevocationListRepository
{
    private readonly IXcaNetDbContextFactory _dbContextFactory;

    public CertificateRevocationListRepository(IXcaNetDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<CertificateRevocationListEntity>> ListAsync(string databasePath, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        return await dbContext.CertificateRevocationLists
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);
    }
}
