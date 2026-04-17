using XcaNet.Contracts.Database;

namespace XcaNet.Parity.Tests;

public sealed class DatabaseSessionStateTests
{
    [Fact]
    public void DatabaseSessionState_ShouldExposeExpectedLifecycleValues()
    {
        Assert.Equal(0, (int)DatabaseSessionState.Closed);
        Assert.Equal(1, (int)DatabaseSessionState.Locked);
        Assert.Equal(2, (int)DatabaseSessionState.Unlocked);
    }
}
