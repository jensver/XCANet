using System.Reflection;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XcaNet.Application.Services;

namespace XcaNet.App.Desktop.Startup;

internal static class StartupDiagnosticsWriter
{
    public static void Write(IServiceProvider services, IConfiguration configuration)
    {
        Directory.CreateDirectory(StartupSupportPaths.GetLogDirectory());

        var sessionService = services.GetRequiredService<IDatabaseSessionService>();
        var diagnostics = sessionService.GetApplicationDiagnosticsAsync(CancellationToken.None).GetAwaiter().GetResult();

        var builder = new StringBuilder()
            .AppendLine($"Timestamp: {DateTimeOffset.UtcNow:u}")
            .AppendLine($"BuildConfiguration: {GetBuildConfiguration()}")
            .AppendLine($"AppVersion: {GetDisplayVersion()}")
            .AppendLine($"ConfiguredBridgePath: {configuration["Crypto:OpenSslBridgePath"] ?? Environment.GetEnvironmentVariable("XCANET_OPENSSL_BRIDGE_PATH") ?? "(none)"}");

        if (diagnostics.IsSuccess && diagnostics.Value is not null)
        {
            builder.AppendLine($"SchemaVersion: {diagnostics.Value.SchemaVersion}")
                .AppendLine($"SessionState: {diagnostics.Value.SessionState}")
                .AppendLine($"ManagedBackendAvailable: {diagnostics.Value.CryptoBackends.ManagedBackendAvailable}")
                .AppendLine($"OpenSslBackendAvailable: {diagnostics.Value.CryptoBackends.OpenSslBackendAvailable}")
                .AppendLine($"OpenSslVersion: {diagnostics.Value.CryptoBackends.OpenSslVersion ?? "(not loaded)"}")
                .AppendLine($"OpenSslCapabilities: {(diagnostics.Value.CryptoBackends.OpenSslCapabilities.Count == 0 ? "(none)" : string.Join(", ", diagnostics.Value.CryptoBackends.OpenSslCapabilities))}")
                .AppendLine($"RoutingSummary: {diagnostics.Value.CryptoBackends.RoutingSummary}");

            if (!string.IsNullOrWhiteSpace(diagnostics.Value.CryptoBackends.OpenSslLoadError))
            {
                builder.AppendLine($"OpenSslLoadError: {diagnostics.Value.CryptoBackends.OpenSslLoadError}");
            }
        }
        else
        {
            builder.AppendLine($"DiagnosticsError: {diagnostics.Message}");
        }

        File.WriteAllText(StartupSupportPaths.GetStartupLogPath(), builder.ToString());
    }

    private static string GetBuildConfiguration()
    {
#if DEBUG
        return "Debug";
#else
        return "Release";
#endif
    }

    private static string GetDisplayVersion()
    {
        return typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(Program).Assembly.GetName().Version?.ToString()
            ?? "0.0.0";
    }
}
