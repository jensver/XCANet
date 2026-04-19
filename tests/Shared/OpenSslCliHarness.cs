using System.Diagnostics;

namespace XcaNet.Tests.Shared;

internal sealed record OpenSslCliResult(
    int ExitCode,
    string StandardOutput,
    string StandardError);

internal static class OpenSslCliHarness
{
    public static bool IsAvailable()
    {
        try
        {
            var result = Run("version");
            return result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public static OpenSslCliResult Run(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "openssl",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            throw new InvalidOperationException("Failed to start the openssl CLI.");
        }

        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new OpenSslCliResult(process.ExitCode, standardOutput, standardError);
    }
}
