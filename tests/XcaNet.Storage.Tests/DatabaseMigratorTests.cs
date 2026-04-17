using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using XcaNet.Storage.Persistence;

namespace XcaNet.Storage.Tests;

public sealed class DatabaseMigratorTests
{
    [Fact]
    public async Task MigrateAsync_ShouldCreateRequiredTables()
    {
        var databasePath = GetDatabasePath();
        var factory = new SqliteXcaNetDbContextFactory();
        var migrator = new DatabaseMigrator(factory);

        await migrator.MigrateAsync(databasePath, CancellationToken.None);

        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync();

        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table'";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        Assert.Contains("PrivateKeys", tables);
        Assert.Contains("Certificates", tables);
        Assert.Contains("CertificateRequests", tables);
        Assert.Contains("CertificateRevocationLists", tables);
        Assert.Contains("Templates", tables);
        Assert.Contains("Authorities", tables);
        Assert.Contains("Tags", tables);
        Assert.Contains("CertificateTags", tables);
        Assert.Contains("AuditEvents", tables);
        Assert.Contains("AppSettings", tables);
        Assert.Contains("DatabaseProfiles", tables);
        Assert.Contains("__EFMigrationsHistory", tables);
    }

    [Fact]
    public async Task MigrateAsync_ShouldRecordAppliedMigration()
    {
        var databasePath = GetDatabasePath();
        var factory = new SqliteXcaNetDbContextFactory();
        var migrator = new DatabaseMigrator(factory);

        await migrator.MigrateAsync(databasePath, CancellationToken.None);

        await using var dbContext = factory.CreateDbContext(databasePath);
        var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();

        Assert.NotEmpty(appliedMigrations);
    }

    private static string GetDatabasePath()
    {
        var path = Path.Combine(Path.GetTempPath(), $"xcanet-storage-{Guid.NewGuid():N}.db");
        return path;
    }
}
