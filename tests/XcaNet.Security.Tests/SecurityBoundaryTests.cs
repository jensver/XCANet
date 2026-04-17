using XcaNet.Security.Protection;

namespace XcaNet.Security.Tests;

public sealed class SecurityBoundaryTests
{
    [Fact]
    public void Marker_ShouldBeCreatable()
    {
        _ = new SecurityBoundary();
    }
}
