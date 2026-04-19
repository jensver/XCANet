namespace XcaNet.Contracts.Crypto;

public sealed record CertificateRevocationListParseRequest(
    byte[] Data,
    CryptoDataFormat Format);
