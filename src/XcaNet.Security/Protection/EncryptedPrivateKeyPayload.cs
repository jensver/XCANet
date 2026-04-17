namespace XcaNet.Security.Protection;

public sealed record EncryptedPrivateKeyPayload(
    byte[] Nonce,
    byte[] Ciphertext,
    byte[] Tag,
    string EncryptionAlgorithm,
    int KeyVersion);
