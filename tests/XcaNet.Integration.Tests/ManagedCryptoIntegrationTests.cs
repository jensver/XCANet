using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XcaNet.Application.DependencyInjection;
using XcaNet.Application.Services;
using XcaNet.Contracts.Crypto;
using XcaNet.Contracts.Crypto.Workflow;
using XcaNet.Contracts.Database;
using XcaNet.Crypto.DotNet.DependencyInjection;
using XcaNet.Storage.Persistence;
using XcaNet.Storage.Repositories;

namespace XcaNet.Integration.Tests;

public sealed class ManagedCryptoIntegrationTests
{
    [Fact]
    public async Task GenerateKey_CreateSelfSignedCa_CreateCsr_SignCsr_ShouldPersistAndParse()
    {
        var databasePath = GetDatabasePath();
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var certificateRepository = provider.GetRequiredService<ICertificateRepository>();
        var certificateRequestRepository = provider.GetRequiredService<ICertificateRequestRepository>();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "M3 Integration"), CancellationToken.None);

        var rootKey = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Root Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var rootCertificate = await service.CreateSelfSignedCaAsync(
            new CreateSelfSignedCaWorkflowRequest(rootKey.Value!.PrivateKeyId, "Root CA", "CN=Root CA", 365),
            CancellationToken.None);
        var leafKey = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Leaf Key", KeyAlgorithmKind.Ecdsa, null, EllipticCurveKind.P256), CancellationToken.None);
        var csr = await service.CreateCertificateSigningRequestAsync(
            new CreateCertificateSigningRequestWorkflowRequest(leafKey.Value!.PrivateKeyId, "Leaf CSR", "CN=leaf.example.test", [new SanEntry("leaf.example.test")]),
            CancellationToken.None);
        var issuedCertificate = await service.SignCertificateSigningRequestAsync(
            new SignStoredCertificateSigningRequestRequest(csr.Value!.CertificateSigningRequestId, rootCertificate.Value!.CertificateId, rootKey.Value.PrivateKeyId, "Leaf Certificate", 180),
            CancellationToken.None);

        Assert.True(rootCertificate.IsSuccess);
        Assert.True(csr.IsSuccess);
        Assert.True(issuedCertificate.IsSuccess);

        var storedCertificate = await certificateRepository.GetAsync(databasePath, issuedCertificate.Value!.CertificateId, CancellationToken.None);
        var storedCsr = await certificateRequestRepository.GetAsync(databasePath, csr.Value.CertificateSigningRequestId, CancellationToken.None);

        Assert.NotNull(storedCertificate);
        Assert.NotNull(storedCsr);
        Assert.Equal("CN=leaf.example.test", issuedCertificate.Value.Details.Subject);
        Assert.Contains("leaf.example.test", csr.Value.Details.SubjectAlternativeNames);
    }

    [Fact]
    public async Task ImportPemAndExportEncryptedPrivateKey_ShouldSucceed()
    {
        var databasePath = GetDatabasePath();
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "M3 Integration"), CancellationToken.None);

        var keyResult = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Export Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var certificateResult = await service.CreateSelfSignedCaAsync(
            new CreateSelfSignedCaWorkflowRequest(keyResult.Value!.PrivateKeyId, "Export CA", "CN=Export CA", 365),
            CancellationToken.None);

        var certificateExport = await service.ExportStoredMaterialAsync(
            new ExportStoredMaterialRequest(CryptoImportKind.Certificate, certificateResult.Value!.CertificateId, CryptoDataFormat.Pem, null, "export-ca"),
            CancellationToken.None);

        Assert.True(certificateExport.IsSuccess);

        var importResult = await service.ImportStoredMaterialAsync(
            new ImportStoredMaterialRequest("Imported PEM Certificate", CryptoImportKind.Certificate, CryptoDataFormat.Pem, System.Text.Encoding.UTF8.GetBytes(certificateExport.Value!.TextRepresentation!), null),
            CancellationToken.None);

        var privateKeyExport = await service.ExportStoredMaterialAsync(
            new ExportStoredMaterialRequest(CryptoImportKind.PrivateKey, keyResult.Value.PrivateKeyId, CryptoDataFormat.Pem, "export-password", "export-key"),
            CancellationToken.None);

        Assert.True(importResult.IsSuccess);
        Assert.Single(importResult.Value!.CertificateIds);
        Assert.True(privateKeyExport.IsSuccess);
        Assert.Contains("ENCRYPTED PRIVATE KEY", privateKeyExport.Value!.TextRepresentation);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddManagedCryptoServices();
        services.AddApplication(new ConfigurationBuilder().Build());
        return services.BuildServiceProvider();
    }

    private static string GetDatabasePath() => Path.Combine(Path.GetTempPath(), $"xcanet-int-m3-{Guid.NewGuid():N}.db");
}
