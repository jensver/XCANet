using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XcaNet.Application.DependencyInjection;
using XcaNet.Application.Services;
using XcaNet.Contracts.Browser;
using XcaNet.Contracts.Crypto;
using XcaNet.Contracts.Crypto.Workflow;
using XcaNet.Contracts.Database;
using XcaNet.Crypto.DotNet.DependencyInjection;

namespace XcaNet.Application.Tests;

public sealed class BrowseWorkflowsTests
{
    [Fact]
    public async Task ListCertificatesAsync_ShouldFilterSearchAndRelationships()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "Browse Test"), CancellationToken.None);
        var issuerKey = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Issuer Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var issuerCertificate = await service.CreateSelfSignedCaAsync(
            new CreateSelfSignedCaWorkflowRequest(issuerKey.Value!.PrivateKeyId, "Issuer CA", "CN=Issuer CA", 365),
            CancellationToken.None);
        var leafKey = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Leaf Key", KeyAlgorithmKind.Ecdsa, null, EllipticCurveKind.P256), CancellationToken.None);
        var csr = await service.CreateCertificateSigningRequestAsync(
            new CreateCertificateSigningRequestWorkflowRequest(leafKey.Value!.PrivateKeyId, "Leaf CSR", "CN=leaf.example.test", [new SanEntry("leaf.example.test")]),
            CancellationToken.None);
        var leafCertificate = await service.SignCertificateSigningRequestAsync(
            new SignStoredCertificateSigningRequestRequest(csr.Value!.CertificateSigningRequestId, issuerCertificate.Value!.CertificateId, issuerKey.Value.PrivateKeyId, "Leaf Certificate", 180),
            CancellationToken.None);

        var allCertificates = await service.ListCertificatesAsync(new CertificateFilterState(null, null, null, null, null, CertificateValidityFilter.All, CertificateAuthorityFilter.All, 30), CancellationToken.None);
        var authorityCertificates = await service.ListCertificatesAsync(new CertificateFilterState(null, null, null, null, null, CertificateValidityFilter.All, CertificateAuthorityFilter.Authorities, 30), CancellationToken.None);
        var searchedCertificates = await service.ListCertificatesAsync(new CertificateFilterState(null, "leaf.example.test", null, null, null, CertificateValidityFilter.All, CertificateAuthorityFilter.All, 30), CancellationToken.None);
        var inspector = await service.GetCertificateInspectorAsync(leafCertificate.Value!.CertificateId, CancellationToken.None);

        Assert.True(allCertificates.IsSuccess);
        Assert.Equal(2, allCertificates.Value!.Count);
        Assert.Contains(allCertificates.Value, x => x.CertificateId == issuerCertificate.Value.CertificateId && x.ChildCertificateCount == 1);
        Assert.True(authorityCertificates.IsSuccess);
        Assert.Single(authorityCertificates.Value!);
        Assert.True(searchedCertificates.IsSuccess);
        var searchedItems = searchedCertificates.Value;
        Assert.NotNull(searchedItems);
        Assert.Single(searchedItems);
        Assert.Equal(leafCertificate.Value!.CertificateId, searchedItems[0].CertificateId);
        Assert.True(inspector.IsSuccess);
        Assert.Equal("Issuer CA", inspector.Value!.Display.IssuerDisplayName);
        Assert.Equal("Leaf Key", inspector.Value.Display.PrivateKeyDisplayName);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_ShouldReturnCurrentCounts()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "Browse Test"), CancellationToken.None);
        var keyResult = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Dashboard Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        await service.CreateCertificateSigningRequestAsync(
            new CreateCertificateSigningRequestWorkflowRequest(keyResult.Value!.PrivateKeyId, "Dashboard CSR", "CN=dashboard.example.test", []),
            CancellationToken.None);

        var summary = await service.GetDashboardSummaryAsync(CancellationToken.None);
        var privateKeys = await service.ListPrivateKeysAsync(CancellationToken.None);
        var requests = await service.ListCertificateSigningRequestsAsync(CancellationToken.None);

        Assert.True(summary.IsSuccess);
        Assert.Equal(1, summary.Value!.PrivateKeys);
        Assert.Equal(1, summary.Value.CertificateSigningRequests);
        Assert.True(privateKeys.IsSuccess);
        Assert.Single(privateKeys.Value!);
        Assert.True(requests.IsSuccess);
        Assert.Single(requests.Value!);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddManagedCryptoServices();
        services.AddApplication(new ConfigurationBuilder().Build());
        return services.BuildServiceProvider();
    }

    private static string GetDatabasePath() => Path.Combine(Path.GetTempPath(), $"xcanet-app-m4-{Guid.NewGuid():N}.db");
}
