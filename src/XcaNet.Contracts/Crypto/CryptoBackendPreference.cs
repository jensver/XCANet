namespace XcaNet.Contracts.Crypto;

public enum CryptoBackendPreference
{
    PreferManaged = 0,
    PreferOpenSsl = 1,
    OpenSslOnly = 2
}
