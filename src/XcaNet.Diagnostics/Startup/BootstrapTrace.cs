using Microsoft.Extensions.Logging;

namespace XcaNet.Diagnostics.Startup;

public sealed class BootstrapTrace
{
    private readonly ILogger<BootstrapTrace> _logger;

    public BootstrapTrace(ILogger<BootstrapTrace> logger)
    {
        _logger = logger;
    }

    public void Write(string message)
    {
        _logger.LogInformation("{Message}", message);
    }
}
