namespace XcaNet.Core.Entities;

public sealed record PrivateKeyRecord(
    Guid Id,
    string DisplayName,
    string Algorithm,
    string PublicKeyFingerprint,
    DateTime CreatedUtc);
