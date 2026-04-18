using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XcaNet.Application.DependencyInjection;
using XcaNet.Application.Services;
using XcaNet.Contracts.Crypto;
using XcaNet.Contracts.Crypto.Workflow;
using XcaNet.Contracts.Database;
using XcaNet.Contracts.Revocation;
using XcaNet.Crypto.Abstractions;
using XcaNet.Crypto.DotNet.DependencyInjection;
using XcaNet.Storage.Repositories;

namespace XcaNet.Integration.Tests;

public sealed class RevocationAndCrlIntegrationTests
{
    [Fact]
    public async Task RevokeCertificateAndGenerateCrl_ShouldPersistAndExposeRevokedEntry()
    {
        var databasePath = GetDatabasePath();
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var certificateRevocationListRepository = provider.GetRequiredService<ICertificateRevocationListRepository>();
        var certificateService = provider.GetRequiredService<ICertificateService>();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "M5 Integration"), CancellationToken.None);

        var issuerKey = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Issuer Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var issuerCertificate = await service.CreateSelfSignedCaAsync(new CreateSelfSignedCaWorkflowRequest(issuerKey.Value!.PrivateKeyId, "Issuer CA", "CN=Issuer CA", 365), CancellationToken.None);
        var leafKey = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Leaf Key", KeyAlgorithmKind.Ecdsa, null, EllipticCurveKind.P256), CancellationToken.None);
        var csr = await service.CreateCertificateSigningRequestAsync(new CreateCertificateSigningRequestWorkflowRequest(leafKey.Value!.PrivateKeyId, "Leaf CSR", "CN=leaf.example.test", [new SanEntry("leaf.example.test")]), CancellationToken.None);
        var leafCertificate = await service.SignCertificateSigningRequestAsync(new SignStoredCertificateSigningRequestRequest(csr.Value!.CertificateSigningRequestId, issuerCertificate.Value!.CertificateId, issuerKey.Value.PrivateKeyId, "Leaf Certificate", 180), CancellationToken.None);
        var revokeResult = await service.RevokeCertificateAsync(new RevokeStoredCertificateRequest(leafCertificate.Value!.CertificateId, CertificateRevocationReason.KeyCompromise, DateTimeOffset.UtcNow), CancellationToken.None);
        var crlResult = await service.GenerateCertificateRevocationListAsync(new GenerateCertificateRevocationListWorkflowRequest(issuerCertificate.Value.CertificateId, issuerKey.Value.PrivateKeyId, "Issuer CA CRL", 7), CancellationToken.None);
        var certificateInspector = await service.GetCertificateInspectorAsync(leafCertificate.Value.CertificateId, CancellationToken.None);
        var crlInspector = await service.GetCertificateRevocationListInspectorAsync(crlResult.Value!.CertificateRevocationListId, CancellationToken.None);

        Assert.True(revokeResult.IsSuccess);
        Assert.True(crlResult.IsSuccess);
        Assert.True(certificateInspector.IsSuccess);
        Assert.True(certificateInspector.Value!.Revocation.IsRevoked);
        Assert.Equal(CertificateRevocationReason.KeyCompromise, certificateInspector.Value.Revocation.Reason);
        Assert.True(crlInspector.IsSuccess);
        Assert.Contains(crlInspector.Value!.RevokedEntries, x => x.CertificateId == Guid.Empty && x.SerialNumber == leafCertificate.Value.Details.SerialNumber);

        var storedCrl = await certificateRevocationListRepository.GetAsync(databasePath, crlResult.Value.CertificateRevocationListId, CancellationToken.None);
        var parsedCrl = await certificateService.ParseCertificateRevocationListAsync(new CertificateRevocationListParseRequest(storedCrl!.DerData, CryptoDataFormat.Der), CancellationToken.None);

        Assert.True(parsedCrl.IsSuccess);
        Assert.Contains(parsedCrl.Value!.RevokedCertificates, x => x.SerialNumber == leafCertificate.Value.Details.SerialNumber);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddManagedCryptoServices();
        services.AddApplication(new ConfigurationBuilder().Build());
        return services.BuildServiceProvider();
    }

    private static string GetDatabasePath() => Path.Combine(Path.GetTempPath(), $"xcanet-int-m5-{Guid.NewGuid():N}.db");
}
