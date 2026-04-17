using XcaNet.Crypto.DotNet;

namespace XcaNet.Crypto.DotNet.Tests;

public sealed class DotNetCryptoBackendTests
{
    [Fact]
    public void Name_ShouldDescribeManagedBackend()
    {
        Assert.Equal("Managed .NET", new DotNetCryptoBackend().Name);
    }
}
