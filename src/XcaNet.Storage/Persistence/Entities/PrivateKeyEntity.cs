namespace XcaNet.Storage.Persistence.Entities;

public sealed class PrivateKeyEntity
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Algorithm { get; set; } = string.Empty;
    public string PublicKeyFingerprint { get; set; } = string.Empty;
    public byte[] EncryptedPkcs8Ciphertext { get; set; } = [];
    public byte[] EncryptionNonce { get; set; } = [];
    public byte[] EncryptionTag { get; set; } = [];
    public string EncryptionAlgorithm { get; set; } = string.Empty;
    public int KeyVersion { get; set; }
    public DateTime CreatedUtc { get; set; }
}
