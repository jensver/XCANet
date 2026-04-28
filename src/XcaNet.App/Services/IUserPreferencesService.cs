namespace XcaNet.App.Services;

public interface IUserPreferencesService
{
    string? DefaultDatabasePath { get; }

    IReadOnlyList<string> RecentDatabases { get; }

    void SetDefaultDatabase(string path);

    void AddRecentDatabase(string path);

    void Save();
}
