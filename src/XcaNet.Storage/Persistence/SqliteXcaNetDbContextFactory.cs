using Microsoft.EntityFrameworkCore;

namespace XcaNet.Storage.Persistence;

public sealed class SqliteXcaNetDbContextFactory : IXcaNetDbContextFactory
{
    public XcaNetDbContext CreateDbContext(string databasePath)
    {
        var options = new DbContextOptionsBuilder<XcaNetDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .EnableSensitiveDataLogging(false)
            .Options;

        return new XcaNetDbContext(options);
    }
}
