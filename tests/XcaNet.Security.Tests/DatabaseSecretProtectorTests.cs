using Microsoft.Extensions.Logging.Abstractions;
using XcaNet.Contracts.Results;
using XcaNet.Security.Protection;

namespace XcaNet.Security.Tests;

public sealed class DatabaseSecretProtectorTests
{
    [Fact]
    public void EncryptDecryptRoundTrip_ShouldReturnOriginalPayload()
    {
        var protector = new DatabaseSecretProtector(NullLogger<DatabaseSecretProtector>.Instance);
        var createResult = protector.CreateProfile("correct horse battery staple");

        Assert.True(createResult.IsSuccess);

        using var unlockedKey = createResult.Value!.Key;
        var plaintext = "pkcs8-private-key"u8.ToArray();
        var encryptResult = protector.EncryptPrivateKey(plaintext, unlockedKey);

        Assert.True(encryptResult.IsSuccess);

        var decryptResult = protector.DecryptPrivateKey(encryptResult.Value!, unlockedKey);

        Assert.True(decryptResult.IsSuccess);
        Assert.Equal(plaintext, decryptResult.Value);
    }

    [Fact]
    public void Unlock_WithWrongPassword_ShouldFail()
    {
        var protector = new DatabaseSecretProtector(NullLogger<DatabaseSecretProtector>.Instance);
        var createResult = protector.CreateProfile("correct horse battery staple");

        Assert.True(createResult.IsSuccess);

        var profile = createResult.Value!.Profile;
        createResult.Value.Key.Dispose();

        var unlockResult = protector.Unlock("wrong password", profile);

        Assert.False(unlockResult.IsSuccess);
        Assert.Equal(OperationErrorCode.InvalidPassword, unlockResult.ErrorCode);
    }
}
