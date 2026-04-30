using System.Text.Json;

namespace XcaNet.App.Services;

public sealed class UserPreferencesService : IUserPreferencesService
{
    private const int MaxRecentCount = 10;

    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "XcaNet",
        "preferences.json");

    private PreferencesData _data;

    public UserPreferencesService()
    {
        _data = Load();
    }

    public string? DefaultDatabasePath => _data.DefaultDatabasePath;

    public IReadOnlyList<string> RecentDatabases => _data.RecentDatabases;

    public void SetDefaultDatabase(string path)
    {
        _data.DefaultDatabasePath = path;
    }

    public void AddRecentDatabase(string path)
    {
        _data.RecentDatabases.Remove(path);
        _data.RecentDatabases.Insert(0, path);
        while (_data.RecentDatabases.Count > MaxRecentCount)
            _data.RecentDatabases.RemoveAt(_data.RecentDatabases.Count - 1);
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static PreferencesData Load()
    {
        if (!File.Exists(FilePath))
            return new PreferencesData();

        try
        {
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<PreferencesData>(json) ?? new PreferencesData();
        }
        catch
        {
            return new PreferencesData();
        }
    }

    private sealed class PreferencesData
    {
        public string? DefaultDatabasePath { get; set; }
        public List<string> RecentDatabases { get; set; } = [];
    }
}
