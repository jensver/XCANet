using XcaNet.Interop.OpenSsl.Bridge;

namespace XcaNet.Interop.Tests;

public sealed class OpenSslBridgeMarkerTests
{
    [Fact]
    public void Marker_ShouldBeCreatable()
    {
        _ = new OpenSslBridgeMarker();
    }
}
