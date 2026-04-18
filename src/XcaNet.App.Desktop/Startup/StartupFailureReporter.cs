using System.Text;

namespace XcaNet.App.Desktop.Startup;

internal static class StartupFailureReporter
{
    public static string Write(Exception exception)
    {
        Directory.CreateDirectory(StartupSupportPaths.GetLogDirectory());

        var message = new StringBuilder()
            .AppendLine($"Timestamp: {DateTimeOffset.UtcNow:u}")
            .AppendLine($"BuildConfiguration: {GetBuildConfiguration()}")
            .AppendLine($"ExceptionType: {exception.GetType().FullName}")
            .AppendLine($"Message: {exception.Message}")
            .AppendLine($"Guidance: {BuildGuidance(exception)}")
            .AppendLine()
            .AppendLine(exception.ToString())
            .ToString();

        var filePath = Path.Combine(
            StartupSupportPaths.GetLogDirectory(),
            $"startup-failure-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.log");

        File.WriteAllText(filePath, message);
        return filePath;
    }

    private static string BuildGuidance(Exception exception)
    {
        if (exception.Message.Contains("RenderTimer", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("environment limitation", StringComparison.OrdinalIgnoreCase))
        {
            return "The desktop runtime could not initialize graphics in the current environment. This does not mean OpenSSL is required. Verify a supported GUI session and consult the startup log.";
        }

        if (exception.Message.Contains("OpenSSL", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("xcanet_ossl_bridge", StringComparison.OrdinalIgnoreCase))
        {
            return "The optional OpenSSL bridge failed to initialize. The managed backend remains the default. Check bridge path, architecture, and native OpenSSL dependencies.";
        }

        return "Review the startup log for details. The managed backend remains the default unless a specific OpenSSL-only operation was requested.";
    }

    private static string GetBuildConfiguration()
    {
#if DEBUG
        return "Debug";
#else
        return "Release";
#endif
    }
}
