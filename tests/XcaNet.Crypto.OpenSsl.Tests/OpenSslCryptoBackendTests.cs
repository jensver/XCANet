using XcaNet.Crypto.OpenSsl;

namespace XcaNet.Crypto.OpenSsl.Tests;

public sealed class OpenSslCryptoBackendTests
{
    [Fact]
    public void Name_ShouldDescribeOpenSslBackend()
    {
        Assert.Equal("OpenSSL", new OpenSslCryptoBackend().Name);
    }
}
