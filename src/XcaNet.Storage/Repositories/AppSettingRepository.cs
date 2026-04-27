using Microsoft.EntityFrameworkCore;
using XcaNet.Storage.Persistence;
using XcaNet.Storage.Persistence.Entities;

namespace XcaNet.Storage.Repositories;

public sealed class AppSettingRepository : IAppSettingRepository
{
    private readonly IXcaNetDbContextFactory _dbContextFactory;

    public AppSettingRepository(IXcaNetDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<string?> GetAsync(string databasePath, string key, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        var entity = await dbContext.AppSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
        return entity?.Value;
    }

    public async Task SetAsync(string databasePath, string key, string value, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        var entity = await dbContext.AppSettings.FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
        if (entity is null)
        {
            dbContext.AppSettings.Add(new AppSettingEntity { Key = key, Value = value, UpdatedUtc = DateTime.UtcNow });
        }
        else
        {
            entity.Value = value;
            entity.UpdatedUtc = DateTime.UtcNow;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
