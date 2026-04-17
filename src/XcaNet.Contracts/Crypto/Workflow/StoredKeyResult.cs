namespace XcaNet.Contracts.Crypto.Workflow;

public sealed record StoredKeyResult(
    Guid PrivateKeyId,
    string Algorithm,
    string PublicKeyFingerprint);
