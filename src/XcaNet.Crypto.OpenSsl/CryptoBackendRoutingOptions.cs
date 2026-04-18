using XcaNet.Contracts.Crypto;

namespace XcaNet.Crypto.OpenSsl;

public sealed class CryptoBackendRoutingOptions
{
    public CryptoBackendPreference DefaultPreference { get; set; } = CryptoBackendPreference.PreferManaged;

    public string? OpenSslBridgePath { get; set; }
}
