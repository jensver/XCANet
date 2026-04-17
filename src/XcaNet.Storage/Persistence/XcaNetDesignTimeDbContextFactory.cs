using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace XcaNet.Storage.Persistence;

public sealed class XcaNetDesignTimeDbContextFactory : IDesignTimeDbContextFactory<XcaNetDbContext>
{
    public XcaNetDbContext CreateDbContext(string[] args)
    {
        var dbPath = Environment.GetEnvironmentVariable("XCANET_MIGRATION_DB_PATH") ?? Path.Combine(Path.GetTempPath(), "xcanet-design.db");
        var options = new DbContextOptionsBuilder<XcaNetDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        return new XcaNetDbContext(options);
    }
}
