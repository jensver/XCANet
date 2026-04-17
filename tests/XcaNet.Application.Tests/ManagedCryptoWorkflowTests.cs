using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XcaNet.Application.DependencyInjection;
using XcaNet.Application.Services;
using XcaNet.Contracts.Crypto;
using XcaNet.Contracts.Crypto.Workflow;
using XcaNet.Contracts.Database;
using XcaNet.Crypto.DotNet.DependencyInjection;

namespace XcaNet.Application.Tests;

public sealed class ManagedCryptoWorkflowTests
{
    [Fact]
    public async Task GenerateStoredKeyAsync_ShouldStoreManagedKey()
    {
        var service = BuildServiceProvider().GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "App Test"), CancellationToken.None);
        var result = await service.GenerateStoredKeyAsync(
            new GenerateStoredKeyRequest("Root Key", KeyAlgorithmKind.Rsa, 3072, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("RSA", result.Value!.Algorithm);
    }

    [Fact]
    public async Task CreateSelfSignedCaAsync_WhenLocked_ShouldFail()
    {
        var service = BuildServiceProvider().GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "App Test"), CancellationToken.None);
        var keyResult = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Root Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        await service.LockDatabaseAsync(CancellationToken.None);

        var certificateResult = await service.CreateSelfSignedCaAsync(
            new CreateSelfSignedCaWorkflowRequest(keyResult.Value!.PrivateKeyId, "Root CA", "CN=Root CA", 365),
            CancellationToken.None);

        Assert.False(certificateResult.IsSuccess);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddManagedCryptoServices();
        services.AddApplication(new ConfigurationBuilder().Build());
        return services.BuildServiceProvider();
    }

    private static string GetDatabasePath() => Path.Combine(Path.GetTempPath(), $"xcanet-app-m3-{Guid.NewGuid():N}.db");
}
