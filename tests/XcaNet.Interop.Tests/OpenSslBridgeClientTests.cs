using XcaNet.Interop.OpenSsl;
using XcaNet.Tests.Shared;

namespace XcaNet.Interop.Tests;

public sealed class OpenSslBridgeClientTests
{
    [Fact]
    public void MissingLibrary_ShouldReportUnavailable()
    {
        var client = new OpenSslBridgeClient(new OpenSslBridgeOptions
        {
            LibraryPath = Path.Combine(Path.GetTempPath(), "missing-xcanet-bridge.dylib")
        });

        Assert.False(client.Diagnostics.IsAvailable);
        Assert.NotNull(client.Diagnostics.LastLoadError);
    }

    [Fact]
    public void BuildAndProbe_ShouldReturnVersionCapabilitiesAndSelfTest()
    {
        var build = OpenSslBridgeTestHarness.BuildNativeBridge();
        Assert.True(build.IsSuccess, build.FailureReason);

        var client = new OpenSslBridgeClient(new OpenSslBridgeOptions
        {
            LibraryPath = build.LibraryPath
        });

        var probe = client.Probe();

        Assert.True(probe.IsSuccess, probe.Message);
        Assert.True(probe.Value!.IsAvailable);
        Assert.Contains("OpenSSL", probe.Value.Version, StringComparison.OrdinalIgnoreCase);
        Assert.True(probe.Value.Capabilities.HasFlag(OpenSslBridgeCapabilities.SupportsCertificateSigningRequestSigning));
        Assert.True(client.SelfTest().IsSuccess);
    }

    [Fact]
    public void RepeatedProbe_ShouldRemainStable()
    {
        var build = OpenSslBridgeTestHarness.BuildNativeBridge();
        Assert.True(build.IsSuccess, build.FailureReason);

        var client = new OpenSslBridgeClient(new OpenSslBridgeOptions
        {
            LibraryPath = build.LibraryPath
        });

        var first = client.Probe();
        var second = client.Probe();
        var third = client.Probe();

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.True(third.IsSuccess);
        Assert.Equal(first.Value!.Version, second.Value!.Version);
        Assert.Equal(second.Value!.Version, third.Value!.Version);
    }

    [Fact]
    public void SignCertificateSigningRequest_WithInvalidArguments_ShouldFailGracefully()
    {
        var build = OpenSslBridgeTestHarness.BuildNativeBridge();
        Assert.True(build.IsSuccess, build.FailureReason);

        var client = new OpenSslBridgeClient(new OpenSslBridgeOptions
        {
            LibraryPath = build.LibraryPath
        });

        var result = client.SignCertificateSigningRequest(
            new OpenSslSignCertificateSigningRequestRequest([], [], [], 0));

        Assert.False(result.IsSuccess);
        Assert.NotEqual(0, (int)result.ErrorCode);
    }
}
