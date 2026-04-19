namespace XcaNet.Security.Protection;

public sealed record DatabaseProtectionProfile(
    string KdfAlgorithm,
    int KdfIterations,
    byte[] KdfSalt,
    byte[] VerifierNonce,
    byte[] VerifierCiphertext,
    byte[] VerifierTag,
    string EncryptionAlgorithm,
    int KeyVersion);
