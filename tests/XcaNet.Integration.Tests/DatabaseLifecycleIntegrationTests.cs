using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XcaNet.Application.DependencyInjection;
using XcaNet.Application.Services;
using XcaNet.Contracts.Database;
using XcaNet.Storage.Persistence;
using XcaNet.Storage.Repositories;

namespace XcaNet.Integration.Tests;

public sealed class DatabaseLifecycleIntegrationTests
{
    [Fact]
    public async Task StorePrivateKey_ShouldPersistEncryptedPayloadAndAuditEvents()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"xcanet-int-{Guid.NewGuid():N}.db");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication(new ConfigurationBuilder().Build());
        var provider = services.BuildServiceProvider();

        var sessionService = provider.GetRequiredService<IDatabaseSessionService>();
        var privateKeyRepository = provider.GetRequiredService<IPrivateKeyRepository>();
        var profileRepository = provider.GetRequiredService<IDatabaseProfileRepository>();
        var dbContextFactory = provider.GetRequiredService<IXcaNetDbContextFactory>();

        var createResult = await sessionService.CreateDatabaseAsync(
            new CreateDatabaseRequest(databasePath, "correct horse battery staple", "Integration Database"),
            CancellationToken.None);

        Assert.True(createResult.IsSuccess);

        var pkcs8Bytes = "test-private-key-material"u8.ToArray();
        var storeResult = await sessionService.StorePrivateKeyAsync(
            new StorePrivateKeyRequest("Imported Key", "RSA", "fingerprint-001", pkcs8Bytes, "import"),
            CancellationToken.None);

        Assert.True(storeResult.IsSuccess);

        var privateKey = await privateKeyRepository.GetAsync(databasePath, storeResult.Value!.PrivateKeyId, CancellationToken.None);
        Assert.NotNull(privateKey);
        Assert.NotEmpty(privateKey!.EncryptedPkcs8Ciphertext);
        Assert.NotEqual("test-private-key-material"u8.ToArray(), privateKey.EncryptedPkcs8Ciphertext);

        var profile = await profileRepository.GetAsync(databasePath, CancellationToken.None);
        Assert.NotNull(profile);

        await using var dbContext = dbContextFactory.CreateDbContext(databasePath);
        Assert.Equal(3, dbContext.AuditEvents.Count());
    }
}
