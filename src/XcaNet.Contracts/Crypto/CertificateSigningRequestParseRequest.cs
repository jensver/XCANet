namespace XcaNet.Contracts.Crypto;

public sealed record CertificateSigningRequestParseRequest(byte[] Data, CryptoDataFormat Format);
