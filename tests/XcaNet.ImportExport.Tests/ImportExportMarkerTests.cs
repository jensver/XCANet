using XcaNet.ImportExport.Formats;

namespace XcaNet.ImportExport.Tests;

public sealed class ImportExportMarkerTests
{
    [Fact]
    public void Marker_ShouldBeCreatable()
    {
        _ = new ImportExportMarker();
    }
}
