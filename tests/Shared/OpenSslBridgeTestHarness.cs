using System.Diagnostics;
using System.Runtime.InteropServices;

namespace XcaNet.Tests.Shared;

internal sealed record OpenSslBridgeBuildResult(
    bool IsSuccess,
    string? LibraryPath,
    string? FailureReason);

internal static class OpenSslBridgeTestHarness
{
    public static OpenSslBridgeBuildResult BuildNativeBridge()
    {
        var repositoryRoot = FindRepositoryRoot();
        if (repositoryRoot is null)
        {
            return new OpenSslBridgeBuildResult(false, null, "Repository root could not be located.");
        }

        var scriptPath = Path.Combine(repositoryRoot, "native", "xcanet_ossl_bridge", "build-bridge.sh");
        if (!File.Exists(scriptPath))
        {
            return new OpenSslBridgeBuildResult(false, null, $"Bridge build script not found: {scriptPath}");
        }

        var outputDirectory = Path.Combine(Path.GetTempPath(), "xcanet-ossl-bridge-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);

        var startInfo = new ProcessStartInfo
        {
            FileName = "/bin/zsh",
            ArgumentList = { scriptPath, outputDirectory },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = repositoryRoot
        };

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return new OpenSslBridgeBuildResult(false, null, "Failed to start bridge build process.");
        }

        process.WaitForExit();
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        if (process.ExitCode != 0)
        {
            return new OpenSslBridgeBuildResult(
                false,
                null,
                $"Bridge build failed with exit code {process.ExitCode}.{Environment.NewLine}{standardOutput}{Environment.NewLine}{standardError}");
        }

        var libraryName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "xcanet_ossl_bridge.dll"
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? "libxcanet_ossl_bridge.dylib"
                : "libxcanet_ossl_bridge.so";

        var libraryPath = Path.Combine(outputDirectory, libraryName);
        return File.Exists(libraryPath)
            ? new OpenSslBridgeBuildResult(true, libraryPath, null)
            : new OpenSslBridgeBuildResult(false, null, $"Expected bridge artifact not found at {libraryPath}.{Environment.NewLine}{standardOutput}{Environment.NewLine}{standardError}");
    }

    private static string? FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "XcaNet.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
