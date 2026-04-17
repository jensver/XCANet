namespace XcaNet.Storage.Persistence;

public interface IDatabaseMigrator
{
    Task MigrateAsync(string databasePath, CancellationToken cancellationToken);
}
