using Microsoft.Extensions.Logging;

namespace XcaNet.Application.Services;

public sealed class StartupWorkflow : IStartupWorkflow
{
    private readonly ILogger<StartupWorkflow> _logger;

    public StartupWorkflow(ILogger<StartupWorkflow> logger)
    {
        _logger = logger;
    }

    public string GetStatus()
    {
        _logger.LogDebug("Application layer initialized.");
        return "Ready";
    }
}
