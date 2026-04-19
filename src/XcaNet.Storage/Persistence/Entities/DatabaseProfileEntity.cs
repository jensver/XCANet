namespace XcaNet.Storage.Persistence.Entities;

public sealed class DatabaseProfileEntity
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string KdfAlgorithm { get; set; } = string.Empty;
    public int KdfIterations { get; set; }
    public byte[] KdfSalt { get; set; } = [];
    public byte[] VerifierNonce { get; set; } = [];
    public byte[] VerifierCiphertext { get; set; } = [];
    public byte[] VerifierTag { get; set; } = [];
    public string EncryptionAlgorithm { get; set; } = string.Empty;
    public int KeyVersion { get; set; }
    public int SchemaVersion { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? LastOpenedUtc { get; set; }
}
