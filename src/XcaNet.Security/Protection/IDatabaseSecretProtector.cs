using XcaNet.Contracts.Results;

namespace XcaNet.Security.Protection;

public interface IDatabaseSecretProtector
{
    OperationResult<(DatabaseProtectionProfile Profile, UnlockedDatabaseKey Key)> CreateProfile(string password);

    OperationResult<UnlockedDatabaseKey> Unlock(string password, DatabaseProtectionProfile profile);

    OperationResult<EncryptedPrivateKeyPayload> EncryptPrivateKey(byte[] plaintextPkcs8, UnlockedDatabaseKey unlockedKey);

    OperationResult<byte[]> DecryptPrivateKey(EncryptedPrivateKeyPayload payload, UnlockedDatabaseKey unlockedKey);
}
