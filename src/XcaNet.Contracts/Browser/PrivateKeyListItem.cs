namespace XcaNet.Contracts.Browser;

public sealed record PrivateKeyListItem(
    Guid PrivateKeyId,
    string DisplayName,
    string Algorithm,
    string PublicKeyFingerprint,
    DateTimeOffset CreatedUtc,
    int LinkedCertificateCount);
