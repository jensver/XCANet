using Microsoft.EntityFrameworkCore;

namespace XcaNet.Storage.Persistence;

public sealed class DatabaseMigrator : IDatabaseMigrator
{
    private readonly IXcaNetDbContextFactory _dbContextFactory;

    public DatabaseMigrator(IXcaNetDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task MigrateAsync(string databasePath, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
