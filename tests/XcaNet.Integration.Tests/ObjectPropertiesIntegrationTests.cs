using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XcaNet.Application.DependencyInjection;
using XcaNet.Application.Services;
using XcaNet.Contracts.Browser;
using XcaNet.Contracts.Crypto;
using XcaNet.Contracts.Crypto.Workflow;
using XcaNet.Contracts.Database;
using XcaNet.Crypto.DotNet.DependencyInjection;

namespace XcaNet.Integration.Tests;

public sealed class ObjectPropertiesIntegrationTests
{
    [Fact]
    public async Task GetObjectPropertiesAsync_ShouldReturnPrivateKeyProperties()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "Props Test"), CancellationToken.None);
        var key = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("My Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        Assert.True(key.IsSuccess, key.Message);

        var result = await service.GetObjectPropertiesAsync(BrowserEntityType.PrivateKey, key.Value!.PrivateKeyId, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Message);
        Assert.Equal("My Key", result.Value!.Name);
        Assert.Null(result.Value.Comment);
        Assert.Equal(BrowserEntityType.PrivateKey, result.Value.Kind);
    }

    [Fact]
    public async Task SaveObjectPropertiesAsync_ShouldUpdateNameAndComment()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "Props Save Test"), CancellationToken.None);
        var key = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Original Name", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        Assert.True(key.IsSuccess, key.Message);

        var saveResult = await service.SaveObjectPropertiesAsync(
            new SaveObjectPropertiesRequest(BrowserEntityType.PrivateKey, key.Value!.PrivateKeyId, "Updated Name", "A comment"),
            CancellationToken.None);

        Assert.True(saveResult.IsSuccess, saveResult.Message);

        var propsResult = await service.GetObjectPropertiesAsync(BrowserEntityType.PrivateKey, key.Value.PrivateKeyId, CancellationToken.None);
        Assert.Equal("Updated Name", propsResult.Value!.Name);
        Assert.Equal("A comment", propsResult.Value.Comment);
    }

    [Fact]
    public async Task SaveObjectPropertiesAsync_ShouldRejectEmptyName()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "Props Validation Test"), CancellationToken.None);
        var key = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        Assert.True(key.IsSuccess, key.Message);

        var result = await service.SaveObjectPropertiesAsync(
            new SaveObjectPropertiesRequest(BrowserEntityType.PrivateKey, key.Value!.PrivateKeyId, "   ", null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(XcaNet.Contracts.Results.OperationErrorCode.ValidationFailed, result.ErrorCode);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddManagedCryptoServices();
        services.AddApplication(new ConfigurationBuilder().Build());
        return services.BuildServiceProvider();
    }

    private static string GetDatabasePath()
        => System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"xcanet-props-{Guid.NewGuid():N}.db");
}
