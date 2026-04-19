using Microsoft.EntityFrameworkCore;
using XcaNet.Storage.Persistence;
using XcaNet.Storage.Persistence.Entities;

namespace XcaNet.Storage.Repositories;

public sealed class DatabaseProfileRepository : IDatabaseProfileRepository
{
    private readonly IXcaNetDbContextFactory _dbContextFactory;

    public DatabaseProfileRepository(IXcaNetDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<bool> ExistsAsync(string databasePath, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        return await dbContext.DatabaseProfiles.AnyAsync(cancellationToken);
    }

    public async Task<DatabaseProfileEntity?> GetAsync(string databasePath, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        return await dbContext.DatabaseProfiles.SingleOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(string databasePath, DatabaseProfileEntity profile, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        dbContext.DatabaseProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateLastOpenedUtcAsync(string databasePath, Guid profileId, DateTime openedUtc, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        var profile = await dbContext.DatabaseProfiles.SingleAsync(x => x.Id == profileId, cancellationToken);
        profile.LastOpenedUtc = openedUtc;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
