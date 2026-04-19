namespace XcaNet.Contracts.Crypto;

public sealed record CertificateParseRequest(byte[] Data, CryptoDataFormat Format);
