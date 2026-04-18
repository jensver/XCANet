namespace XcaNet.Contracts.Browser;

public sealed record CertificateRawFields(
    string Subject,
    string Issuer,
    string SerialNumber,
    DateTimeOffset NotBefore,
    DateTimeOffset NotAfter,
    string Sha1Thumbprint,
    string Sha256Thumbprint,
    string KeyAlgorithm);
