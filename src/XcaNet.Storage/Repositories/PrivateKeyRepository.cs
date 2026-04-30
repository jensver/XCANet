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

    public async Task UpdateDisplayNameAsync(string databasePath, Guid privateKeyId, string newName, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        var key = await dbContext.PrivateKeys.SingleAsync(x => x.Id == privateKeyId, cancellationToken);
        key.DisplayName = newName;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateCommentAsync(string databasePath, Guid privateKeyId, string? comment, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        var key = await dbContext.PrivateKeys.SingleAsync(x => x.Id == privateKeyId, cancellationToken);
        key.Comment = comment;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
