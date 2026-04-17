using XcaNet.Contracts;

namespace XcaNet.Parity.Tests;

public sealed class AppStatusTests
{
    [Fact]
    public void AppStatus_ShouldPreserveValue()
    {
        Assert.Equal("Ready", new AppStatus("Ready").Value);
    }
}
