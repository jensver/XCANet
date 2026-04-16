using XcaNet.Crypto.Abstractions;

namespace XcaNet.Crypto.OpenSsl;

public sealed class OpenSslCryptoBackend : ICryptoBackend
{
    public string Name => "OpenSSL";
}
