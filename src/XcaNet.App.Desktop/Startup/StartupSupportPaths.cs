namespace XcaNet.App.Desktop.Startup;

internal static class StartupSupportPaths
{
    public static string GetLogDirectory()
    {
        var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(basePath, "XcaNet", "logs");
    }

    public static string GetStartupLogPath()
    {
        return Path.Combine(GetLogDirectory(), "startup.log");
    }
}
