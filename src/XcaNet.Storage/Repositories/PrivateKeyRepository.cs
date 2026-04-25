using Microsoft.EntityFrameworkCore;
using XcaNet.Storage.Persistence;
using XcaNet.Storage.Persistence.Entities;

namespace XcaNet.Storage.Repositories;

public sealed class PrivateKeyRepository : IPrivateKeyRepository
{
    private readonly IXcaNetDbContextFactory _dbContextFactory;

    public PrivateKeyRepository(IXcaNetDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task AddAsync(string databasePath, PrivateKeyEntity privateKey, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        dbContext.PrivateKeys.Add(privateKey);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PrivateKeyEntity?> GetAsync(string databasePath, Guid privateKeyId, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        return await dbContext.PrivateKeys.SingleOrDefaultAsync(x => x.Id == privateKeyId, cancellationToken);
    }

    public async Task<IReadOnlyList<PrivateKeyEntity>> ListAsync(string databasePath, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        return await dbContext.PrivateKeys
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(string databasePath, Guid privateKeyId, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        var existing = await dbContext.PrivateKeys.SingleOrDefaultAsync(x => x.Id == privateKeyId, cancellationToken);
        if (existing is null)
            return false;

        dbContext.PrivateKeys.Remove(existing);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
