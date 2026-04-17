namespace XcaNet.Contracts.Crypto;

public sealed record SelfSignedCaCertificateRequest(
    string SubjectName,
    byte[] Pkcs8PrivateKey,
    string KeyAlgorithm,
    int ValidityDays);
