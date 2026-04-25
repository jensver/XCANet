using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XcaNet.Application.DependencyInjection;
using XcaNet.Application.Services;
using XcaNet.Contracts.Crypto;
using XcaNet.Contracts.Crypto.Workflow;
using XcaNet.Contracts.Database;
using XcaNet.Crypto.DotNet.DependencyInjection;

namespace XcaNet.Application.Tests;

public sealed class CsrWorkflowTests
{
    [Fact]
    public async Task CreateCertificateSigningRequestAsync_WithAnyStoredKey_ShouldSucceed()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "CSR Test"), CancellationToken.None);
        var keyResult = await service.GenerateStoredKeyAsync(
            new GenerateStoredKeyRequest("Service Key", KeyAlgorithmKind.Rsa, 3072, null),
            CancellationToken.None);

        // Act — create CSR directly from key ID without pre-navigating the Private Keys tab
        var result = await service.CreateCertificateSigningRequestAsync(
            new CreateCertificateSigningRequestWorkflowRequest(
                keyResult.Value!.PrivateKeyId,
                "Service CSR",
                "CN=service.example.test",
                []),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(keyResult.Value.PrivateKeyId, result.Value.PrivateKeyId);
        Assert.NotEqual(Guid.Empty, result.Value.CertificateSigningRequestId);
    }

    [Fact]
    public async Task CreateCertificateSigningRequestAsync_WithSecondKeyInStore_ShouldSucceed()
    {
        // Arrange — two keys stored; CSR targets the non-first key
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "CSR Test"), CancellationToken.None);
        await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("First Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var secondKeyResult = await service.GenerateStoredKeyAsync(
            new GenerateStoredKeyRequest("Second Key", KeyAlgorithmKind.Ecdsa, null, EllipticCurveKind.P256),
            CancellationToken.None);

        // Act
        var result = await service.CreateCertificateSigningRequestAsync(
            new CreateCertificateSigningRequestWorkflowRequest(
                secondKeyResult.Value!.PrivateKeyId,
                "Second Key CSR",
                "CN=second.example.test",
                []),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(secondKeyResult.Value.PrivateKeyId, result.Value.PrivateKeyId);
        Assert.NotEqual(Guid.Empty, result.Value.CertificateSigningRequestId);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddManagedCryptoServices();
        services.AddApplication(new ConfigurationBuilder().Build());
        return services.BuildServiceProvider();
    }

    private static string GetDatabasePath() => Path.Combine(Path.GetTempPath(), $"xcanet-csr-{Guid.NewGuid():N}.db");
}
