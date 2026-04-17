using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using XcaNet.Contracts.Results;

namespace XcaNet.Security.Protection;

public sealed class DatabaseSecretProtector : IDatabaseSecretProtector
{
    private const int SaltLength = 16;
    private const int NonceLength = 12;
    private const int KeyLength = 32;
    private const int TagLength = 16;
    private const int DefaultIterations = 210_000;
    private const string KdfAlgorithm = "PBKDF2-SHA256";
    private const string EncryptionAlgorithm = "AES-256-GCM";
    private static readonly byte[] VerifierPlaintext = "xcanet-db-verifier"u8.ToArray();

    private readonly ILogger<DatabaseSecretProtector> _logger;

    public DatabaseSecretProtector(ILogger<DatabaseSecretProtector> logger)
    {
        _logger = logger;
    }

    public OperationResult<(DatabaseProtectionProfile Profile, UnlockedDatabaseKey Key)> CreateProfile(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return OperationResult<(DatabaseProtectionProfile, UnlockedDatabaseKey)>.Failure(
                OperationErrorCode.ValidationFailed,
                "A master password is required.");
        }

        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var nonce = RandomNumberGenerator.GetBytes(NonceLength);
        var keyBytes = DeriveKey(password, salt, DefaultIterations);
        var ciphertext = new byte[VerifierPlaintext.Length];
        var tag = new byte[TagLength];

        try
        {
            using var aes = new AesGcm(keyBytes, TagLength);
            aes.Encrypt(nonce, VerifierPlaintext, ciphertext, tag);
        }
        catch (Exception ex) when (ex is CryptographicException or ArgumentException)
        {
            CryptographicOperations.ZeroMemory(keyBytes);
            _logger.LogError(ex, "Failed to create database protection profile.");
            return OperationResult<(DatabaseProtectionProfile, UnlockedDatabaseKey)>.Failure(
                OperationErrorCode.StorageFailure,
                "Failed to initialize database protection.");
        }

        var profile = new DatabaseProtectionProfile(
            KdfAlgorithm,
            DefaultIterations,
            salt,
            nonce,
            ciphertext,
            tag,
            EncryptionAlgorithm,
            1);

        var unlockedKey = new UnlockedDatabaseKey(keyBytes, profile.KeyVersion, profile.KdfAlgorithm, profile.KdfIterations);
        return OperationResult<(DatabaseProtectionProfile, UnlockedDatabaseKey)>.Success(
            (profile, unlockedKey),
            "Database protection profile created.");
    }

    public OperationResult<UnlockedDatabaseKey> Unlock(string password, DatabaseProtectionProfile profile)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return OperationResult<UnlockedDatabaseKey>.Failure(OperationErrorCode.ValidationFailed, "A master password is required.");
        }

        var keyBytes = DeriveKey(password, profile.KdfSalt, profile.KdfIterations);
        var plaintext = new byte[profile.VerifierCiphertext.Length];

        try
        {
            using var aes = new AesGcm(keyBytes, TagLength);
            aes.Decrypt(profile.VerifierNonce, profile.VerifierCiphertext, profile.VerifierTag, plaintext);

            if (!CryptographicOperations.FixedTimeEquals(plaintext, VerifierPlaintext))
            {
                CryptographicOperations.ZeroMemory(keyBytes);
                return OperationResult<UnlockedDatabaseKey>.Failure(OperationErrorCode.InvalidPassword, "The database password is invalid.");
            }
        }
        catch (CryptographicException)
        {
            CryptographicOperations.ZeroMemory(keyBytes);
            return OperationResult<UnlockedDatabaseKey>.Failure(OperationErrorCode.InvalidPassword, "The database password is invalid.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }

        return OperationResult<UnlockedDatabaseKey>.Success(
            new UnlockedDatabaseKey(keyBytes, profile.KeyVersion, profile.KdfAlgorithm, profile.KdfIterations),
            "Database unlocked.");
    }

    public OperationResult<EncryptedPrivateKeyPayload> EncryptPrivateKey(byte[] plaintextPkcs8, UnlockedDatabaseKey unlockedKey)
    {
        if (plaintextPkcs8.Length == 0)
        {
            return OperationResult<EncryptedPrivateKeyPayload>.Failure(
                OperationErrorCode.ValidationFailed,
                "Private key data is required.");
        }

        var nonce = RandomNumberGenerator.GetBytes(NonceLength);
        var ciphertext = new byte[plaintextPkcs8.Length];
        var tag = new byte[TagLength];

        try
        {
            using var aes = new AesGcm(unlockedKey.KeyBytes, TagLength);
            aes.Encrypt(nonce, plaintextPkcs8, ciphertext, tag);
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Failed to encrypt a private key payload.");
            return OperationResult<EncryptedPrivateKeyPayload>.Failure(
                OperationErrorCode.StorageFailure,
                "Failed to encrypt private key data.");
        }

        return OperationResult<EncryptedPrivateKeyPayload>.Success(
            new EncryptedPrivateKeyPayload(nonce, ciphertext, tag, EncryptionAlgorithm, unlockedKey.KeyVersion),
            "Private key encrypted.");
    }

    public OperationResult<byte[]> DecryptPrivateKey(EncryptedPrivateKeyPayload payload, UnlockedDatabaseKey unlockedKey)
    {
        var plaintext = new byte[payload.Ciphertext.Length];

        try
        {
            using var aes = new AesGcm(unlockedKey.KeyBytes, TagLength);
            aes.Decrypt(payload.Nonce, payload.Ciphertext, payload.Tag, plaintext);
        }
        catch (CryptographicException)
        {
            CryptographicOperations.ZeroMemory(plaintext);
            return OperationResult<byte[]>.Failure(
                OperationErrorCode.InvalidPassword,
                "The database password is invalid or the private key payload is corrupted.");
        }

        return OperationResult<byte[]>.Success(plaintext, "Private key decrypted.");
    }

    private static byte[] DeriveKey(string password, byte[] salt, int iterations)
    {
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, KeyLength);
    }
}
