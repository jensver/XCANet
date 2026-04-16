using XcaNet.Application.Services;

namespace XcaNet.Integration.Tests;

public sealed class SolutionSmokeTests
{
    [Fact]
    public void StartupWorkflowContract_ShouldRemainStable()
    {
        Assert.Equal(typeof(string), typeof(IStartupWorkflow).GetMethod(nameof(IStartupWorkflow.GetStatus))?.ReturnType);
    }
}
