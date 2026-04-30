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

    public async Task AddAsync(string databasePath, CertificateRevocationListEntity certificateRevocationList, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        dbContext.CertificateRevocationLists.Add(certificateRevocationList);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<CertificateRevocationListEntity?> GetAsync(string databasePath, Guid certificateRevocationListId, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        return await dbContext.CertificateRevocationLists
            .AsNoTracking()
            .Include(x => x.RevokedEntries)
            .SingleOrDefaultAsync(x => x.Id == certificateRevocationListId, cancellationToken);
    }

    public async Task<IReadOnlyList<CertificateRevocationListEntity>> ListAsync(string databasePath, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        return await dbContext.CertificateRevocationLists
            .AsNoTracking()
            .Include(x => x.RevokedEntries)
            .OrderByDescending(x => x.ThisUpdateUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateDisplayNameAsync(string databasePath, Guid certificateRevocationListId, string newName, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        var crl = await dbContext.CertificateRevocationLists.SingleAsync(x => x.Id == certificateRevocationListId, cancellationToken);
        crl.DisplayName = newName;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateCommentAsync(string databasePath, Guid certificateRevocationListId, string? comment, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        var crl = await dbContext.CertificateRevocationLists.SingleAsync(x => x.Id == certificateRevocationListId, cancellationToken);
        crl.Comment = comment;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
