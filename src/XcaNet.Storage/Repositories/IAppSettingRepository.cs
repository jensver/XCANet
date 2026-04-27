namespace XcaNet.Storage.Repositories;

public interface IAppSettingRepository
{
    Task<string?> GetAsync(string databasePath, string key, CancellationToken cancellationToken);

    Task SetAsync(string databasePath, string key, string value, CancellationToken cancellationToken);
}
