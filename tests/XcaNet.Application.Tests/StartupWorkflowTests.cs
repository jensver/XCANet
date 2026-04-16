using Microsoft.Extensions.Logging.Abstractions;
using XcaNet.Application.Services;

namespace XcaNet.Application.Tests;

public sealed class StartupWorkflowTests
{
    [Fact]
    public void GetStatus_ShouldReturnReady()
    {
        var workflow = new StartupWorkflow(NullLogger<StartupWorkflow>.Instance);

        Assert.Equal("Ready", workflow.GetStatus());
    }
}
