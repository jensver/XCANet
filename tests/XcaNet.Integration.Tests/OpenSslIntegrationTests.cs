using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XcaNet.Application.DependencyInjection;
using XcaNet.Application.Services;
using XcaNet.Contracts.Crypto;
using XcaNet.Contracts.Crypto.Workflow;
using XcaNet.Contracts.Database;
using XcaNet.Crypto.OpenSsl;
using XcaNet.Crypto.OpenSsl.DependencyInjection;
using XcaNet.Tests.Shared;

namespace XcaNet.Integration.Tests;

public sealed class OpenSslIntegrationTests
{
    [Fact]
    public async Task ApplicationStack_ShouldFallBackToManagedWhenOpenSslIsUnavailable()
    {
        using var provider = BuildServiceProvider(options =>
        {
            options.DefaultPreference = CryptoBackendPreference.PreferOpenSsl;
            options.OpenSslBridgePath = Path.Combine(Path.GetTempPath(), "missing-xcanet-bridge.dylib");
        });

        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "M6 Missing"), CancellationToken.None);
        var issuerKey = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Issuer Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var issuerCertificate = await service.CreateSelfSignedCaAsync(new CreateSelfSignedCaWorkflowRequest(issuerKey.Value!.PrivateKeyId, "Issuer CA", "CN=Issuer CA", 365), CancellationToken.None);
        var leafKey = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Leaf Key", KeyAlgorithmKind.Ecdsa, null, EllipticCurveKind.P256), CancellationToken.None);
        var csr = await service.CreateCertificateSigningRequestAsync(new CreateCertificateSigningRequestWorkflowRequest(leafKey.Value!.PrivateKeyId, "Leaf CSR", "CN=leaf.example.test", [new SanEntry("leaf.example.test")]), CancellationToken.None);
        var leafCertificate = await service.SignCertificateSigningRequestAsync(new SignStoredCertificateSigningRequestRequest(csr.Value!.CertificateSigningRequestId, issuerCertificate.Value!.CertificateId, issuerKey.Value.PrivateKeyId, "Leaf Certificate", 180), CancellationToken.None);
        var diagnostics = await service.GetApplicationDiagnosticsAsync(CancellationToken.None);

        Assert.True(leafCertificate.IsSuccess, leafCertificate.Message);
        Assert.Equal(CryptoBackendKind.Managed, leafCertificate.Value!.BackendUsed);
        Assert.True(diagnostics.IsSuccess, diagnostics.Message);
        Assert.False(diagnostics.Value!.CryptoBackends.OpenSslBackendAvailable);
        Assert.Contains("fallback", diagnostics.Value.CryptoBackends.RoutingSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplicationStack_ShouldUseOpenSslWhenBridgeIsPresent()
    {
        var build = OpenSslBridgeTestHarness.BuildNativeBridge();
        Assert.True(build.IsSuccess, build.FailureReason);

        using var provider = BuildServiceProvider(options =>
        {
            options.DefaultPreference = CryptoBackendPreference.PreferOpenSsl;
            options.OpenSslBridgePath = build.LibraryPath;
        });

        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "M6 Present"), CancellationToken.None);
        var issuerKey = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Issuer Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var issuerCertificate = await service.CreateSelfSignedCaAsync(new CreateSelfSignedCaWorkflowRequest(issuerKey.Value!.PrivateKeyId, "Issuer CA", "CN=Issuer CA", 365), CancellationToken.None);
        var leafKey = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Leaf Key", KeyAlgorithmKind.Ecdsa, null, EllipticCurveKind.P256), CancellationToken.None);
        var csr = await service.CreateCertificateSigningRequestAsync(new CreateCertificateSigningRequestWorkflowRequest(leafKey.Value!.PrivateKeyId, "Leaf CSR", "CN=leaf.example.test", [new SanEntry("leaf.example.test")]), CancellationToken.None);
        var leafCertificate = await service.SignCertificateSigningRequestAsync(new SignStoredCertificateSigningRequestRequest(csr.Value!.CertificateSigningRequestId, issuerCertificate.Value!.CertificateId, issuerKey.Value.PrivateKeyId, "Leaf Certificate", 180), CancellationToken.None);
        var revoke = await service.RevokeCertificateAsync(new RevokeStoredCertificateRequest(leafCertificate.Value!.CertificateId, Contracts.Revocation.CertificateRevocationReason.KeyCompromise, DateTimeOffset.UtcNow), CancellationToken.None);
        var crl = await service.GenerateCertificateRevocationListAsync(new GenerateCertificateRevocationListWorkflowRequest(issuerCertificate.Value.CertificateId, issuerKey.Value.PrivateKeyId, "Issuer CRL", 7), CancellationToken.None);
        var diagnostics = await service.GetApplicationDiagnosticsAsync(CancellationToken.None);

        Assert.True(leafCertificate.IsSuccess, leafCertificate.Message);
        Assert.Equal(CryptoBackendKind.OpenSsl, leafCertificate.Value!.BackendUsed);
        Assert.True(revoke.IsSuccess, revoke.Message);
        Assert.True(crl.IsSuccess, crl.Message);
        Assert.True(diagnostics.IsSuccess, diagnostics.Message);
        Assert.True(diagnostics.Value!.CryptoBackends.OpenSslBackendAvailable);
        Assert.Contains("Loaded bridge", diagnostics.Value.CryptoBackends.RoutingSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostics.Value.SchemaVersion);
    }

    [Fact]
    public async Task ApplicationStack_ShouldRemainManagedByDefaultEvenWhenBridgeIsPresent()
    {
        var build = OpenSslBridgeTestHarness.BuildNativeBridge();
        Assert.True(build.IsSuccess, build.FailureReason);

        using var provider = BuildServiceProvider(options =>
        {
            options.OpenSslBridgePath = build.LibraryPath;
        });

        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "M7 Managed Default"), CancellationToken.None);
        var issuerKey = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Issuer Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var issuerCertificate = await service.CreateSelfSignedCaAsync(new CreateSelfSignedCaWorkflowRequest(issuerKey.Value!.PrivateKeyId, "Issuer CA", "CN=Issuer CA", 365), CancellationToken.None);
        var leafKey = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Leaf Key", KeyAlgorithmKind.Ecdsa, null, EllipticCurveKind.P256), CancellationToken.None);
        var csr = await service.CreateCertificateSigningRequestAsync(new CreateCertificateSigningRequestWorkflowRequest(leafKey.Value!.PrivateKeyId, "Leaf CSR", "CN=leaf.example.test", [new SanEntry("leaf.example.test")]), CancellationToken.None);
        var leafCertificate = await service.SignCertificateSigningRequestAsync(new SignStoredCertificateSigningRequestRequest(csr.Value!.CertificateSigningRequestId, issuerCertificate.Value!.CertificateId, issuerKey.Value.PrivateKeyId, "Leaf Certificate", 180), CancellationToken.None);

        Assert.True(leafCertificate.IsSuccess, leafCertificate.Message);
        Assert.Equal(CryptoBackendKind.Managed, leafCertificate.Value!.BackendUsed);
    }

    [Fact]
    public async Task Diagnostics_ShouldReportReleaseCandidateVersion()
    {
        using var provider = BuildServiceProvider(_ => { });
        var service = provider.GetRequiredService<IDatabaseSessionService>();

        var diagnostics = await service.GetApplicationDiagnosticsAsync(CancellationToken.None);

        Assert.True(diagnostics.IsSuccess, diagnostics.Message);
        Assert.Equal("0.1.0", diagnostics.Value!.AppVersion);
    }

    private static ServiceProvider BuildServiceProvider(Action<CryptoBackendRoutingOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddXcaNetCryptoServices(new ConfigurationBuilder().Build(), configure);
        services.AddApplication(new ConfigurationBuilder().Build());
        return services.BuildServiceProvider();
    }

    private static string GetDatabasePath() => Path.Combine(Path.GetTempPath(), $"xcanet-int-m6-{Guid.NewGuid():N}.db");
}
