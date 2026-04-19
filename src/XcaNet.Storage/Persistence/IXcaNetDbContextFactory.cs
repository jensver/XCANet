namespace XcaNet.Storage.Persistence;

public interface IXcaNetDbContextFactory
{
    XcaNetDbContext CreateDbContext(string databasePath);
}
