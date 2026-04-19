using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XcaNet.Application.DependencyInjection;
using XcaNet.Application.Services;
using XcaNet.Contracts.Crypto;
using XcaNet.Contracts.Crypto.Workflow;
using XcaNet.Contracts.Database;
using XcaNet.Contracts.Revocation;
using XcaNet.Crypto.DotNet.DependencyInjection;

namespace XcaNet.Application.Tests;

public sealed class RevocationWorkflowTests
{
    [Fact]
    public async Task RevokeCertificateAsync_WhenAlreadyRevoked_ShouldFail()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "Revocation Test"), CancellationToken.None);
        var caKey = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("CA Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var caCertificate = await service.CreateSelfSignedCaAsync(new CreateSelfSignedCaWorkflowRequest(caKey.Value!.PrivateKeyId, "CA", "CN=CA", 365), CancellationToken.None);

        var firstRevocation = await service.RevokeCertificateAsync(
            new RevokeStoredCertificateRequest(caCertificate.Value!.CertificateId, CertificateRevocationReason.KeyCompromise, DateTimeOffset.UtcNow),
            CancellationToken.None);
        var secondRevocation = await service.RevokeCertificateAsync(
            new RevokeStoredCertificateRequest(caCertificate.Value.CertificateId, CertificateRevocationReason.KeyCompromise, DateTimeOffset.UtcNow),
            CancellationToken.None);

        Assert.True(firstRevocation.IsSuccess);
        Assert.False(secondRevocation.IsSuccess);
    }

    [Fact]
    public async Task GenerateCertificateRevocationListAsync_WhenIssuerIsNotCa_ShouldFail()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "Revocation Test"), CancellationToken.None);
        var issuerKey = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Issuer Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var issuerCertificate = await service.CreateSelfSignedCaAsync(new CreateSelfSignedCaWorkflowRequest(issuerKey.Value!.PrivateKeyId, "Issuer CA", "CN=Issuer CA", 365), CancellationToken.None);
        var leafKey = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Leaf Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var csr = await service.CreateCertificateSigningRequestAsync(
            new CreateCertificateSigningRequestWorkflowRequest(leafKey.Value!.PrivateKeyId, "Leaf CSR", "CN=leaf.example.test", []),
            CancellationToken.None);
        var leafCertificate = await service.SignCertificateSigningRequestAsync(
            new SignStoredCertificateSigningRequestRequest(csr.Value!.CertificateSigningRequestId, issuerCertificate.Value!.CertificateId, issuerKey.Value.PrivateKeyId, "Leaf Certificate", 180),
            CancellationToken.None);

        var result = await service.GenerateCertificateRevocationListAsync(
            new GenerateCertificateRevocationListWorkflowRequest(leafCertificate.Value!.CertificateId, leafKey.Value.PrivateKeyId, "Leaf CRL", 7),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddManagedCryptoServices();
        services.AddApplication(new ConfigurationBuilder().Build());
        return services.BuildServiceProvider();
    }

    private static string GetDatabasePath() => Path.Combine(Path.GetTempPath(), $"xcanet-app-m5-{Guid.NewGuid():N}.db");
}
