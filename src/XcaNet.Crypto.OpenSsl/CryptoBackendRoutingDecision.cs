using XcaNet.Contracts.Crypto;

namespace XcaNet.Crypto.OpenSsl;

public sealed record CryptoBackendRoutingDecision(
    CryptoBackendPreference Preference,
    CryptoBackendKind BackendToUse,
    bool FellBackToManaged,
    string Reason);
