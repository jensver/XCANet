using XcaNet.Crypto.Abstractions;

namespace XcaNet.Crypto.DotNet;

public sealed class DotNetCryptoBackend : ICryptoBackend
{
    public string Name => "Managed .NET";
}
