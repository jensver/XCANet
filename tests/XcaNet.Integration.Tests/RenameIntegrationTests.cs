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

public sealed class RenameIntegrationTests
{
    [Fact]
    public async Task RenameStoredItemAsync_ShouldRenamePrivateKey()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "Rename Test"), CancellationToken.None);
        var key = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Original Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        Assert.True(key.IsSuccess, key.Message);

        var result = await service.RenameStoredItemAsync(
            new RenameStoredItemRequest(BrowserEntityType.PrivateKey, key.Value!.PrivateKeyId, "Renamed Key"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Message);

        var keys = await service.ListPrivateKeysAsync(CancellationToken.None);
        Assert.Contains(keys.Value!, x => x.DisplayName == "Renamed Key");
        Assert.DoesNotContain(keys.Value!, x => x.DisplayName == "Original Key");
    }

    [Fact]
    public async Task RenameStoredItemAsync_ShouldRenameCertificate()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "Rename Cert"), CancellationToken.None);
        var key = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("CA Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var cert = await service.CreateSelfSignedCaAsync(new CreateSelfSignedCaWorkflowRequest(key.Value!.PrivateKeyId, "Root CA", "CN=Root CA", 365), CancellationToken.None);

        var result = await service.RenameStoredItemAsync(
            new RenameStoredItemRequest(BrowserEntityType.Certificate, cert.Value!.CertificateId, "Renamed Root CA"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Message);

        var certs = await service.ListCertificatesAsync(new XcaNet.Contracts.Browser.CertificateFilterState(null, null, null, null, null, XcaNet.Contracts.Browser.CertificateValidityFilter.All, XcaNet.Contracts.Browser.CertificateAuthorityFilter.All, 30), CancellationToken.None);
        Assert.Contains(certs.Value!, x => x.DisplayName == "Renamed Root CA");
    }

    [Fact]
    public async Task RenameStoredItemAsync_ShouldRejectEmptyName()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "Rename Empty"), CancellationToken.None);
        var key = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);

        var result = await service.RenameStoredItemAsync(
            new RenameStoredItemRequest(BrowserEntityType.PrivateKey, key.Value!.PrivateKeyId, "   "),
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
        => System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"xcanet-rename-{Guid.NewGuid():N}.db");
}
