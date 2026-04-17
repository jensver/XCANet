using XcaNet.Storage.Persistence;

namespace XcaNet.Storage.Tests;

public sealed class RepositoryMarkerTests
{
    [Fact]
    public void Marker_ShouldBeCreatable()
    {
        _ = new RepositoryMarker();
    }
}
