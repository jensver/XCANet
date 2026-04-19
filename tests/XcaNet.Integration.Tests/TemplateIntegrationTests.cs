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

public sealed class TemplateIntegrationTests
{
    [Fact]
    public async Task TemplateDrivenCsrAndIssuance_ShouldReflectTemplateDefaults()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "Template Integration"), CancellationToken.None);

        var template = await service.SaveTemplateAsync(
            new SaveTemplateRequest(
                null,
                "Leaf Service",
                null,
                true,
                true,
                TemplateIntendedUsage.EndEntityCertificate,
                "CN=leaf.example.test",
                ["leaf.example.test", "api.example.test"],
                KeyAlgorithmKind.Ecdsa,
                null,
                EllipticCurveKind.P256,
                "SHA-256",
                180,
                false,
                null,
                ["DigitalSignature", "KeyEncipherment"],
                ["Server Authentication"]),
            CancellationToken.None);

        var issuerKey = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Issuer Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var issuerCertificate = await service.CreateSelfSignedCaAsync(
            new CreateSelfSignedCaWorkflowRequest(issuerKey.Value!.PrivateKeyId, "Issuer CA", "CN=Issuer CA", 3650),
            CancellationToken.None);
        var leafKey = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Leaf Key", KeyAlgorithmKind.Ecdsa, null, EllipticCurveKind.P256), CancellationToken.None);
        var csr = await service.CreateCertificateSigningRequestAsync(
            new CreateCertificateSigningRequestWorkflowRequest(
                leafKey.Value!.PrivateKeyId,
                string.Empty,
                string.Empty,
                [],
                false,
                null,
                [],
                [],
                template.Value!.TemplateId),
            CancellationToken.None);
        var issued = await service.SignCertificateSigningRequestAsync(
            new SignStoredCertificateSigningRequestRequest(
                csr.Value!.CertificateSigningRequestId,
                issuerCertificate.Value!.CertificateId,
                issuerKey.Value.PrivateKeyId,
                string.Empty,
                0,
                template.Value.TemplateId),
            CancellationToken.None);

        Assert.True(csr.IsSuccess);
        var csrValue = csr.Value;
        Assert.NotNull(csrValue);
        Assert.Equal("CN=leaf.example.test", csrValue!.Details.Subject);
        Assert.Contains("api.example.test", csrValue.Details.SubjectAlternativeNames!);
        Assert.Contains("KeyEncipherment", csrValue.Details.KeyUsages!);
        Assert.True(issued.IsSuccess);
        Assert.Equal("CN=leaf.example.test", issued.Value!.Details.Subject);
        Assert.False(issued.Value.Details.IsCertificateAuthority);
    }

    [Fact]
    public async Task IntermediateTemplateFlow_ShouldProduceCaCertificate()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "Intermediate Template"), CancellationToken.None);

        var template = await service.SaveTemplateAsync(
            new SaveTemplateRequest(
                null,
                "Intermediate CA",
                null,
                false,
                true,
                TemplateIntendedUsage.IntermediateCa,
                "CN=Intermediate CA",
                [],
                KeyAlgorithmKind.Rsa,
                3072,
                null,
                "SHA-256",
                825,
                true,
                0,
                ["KeyCertSign", "CrlSign", "DigitalSignature"],
                []),
            CancellationToken.None);

        var issuerKey = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Root Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var issuerCertificate = await service.CreateSelfSignedCaAsync(
            new CreateSelfSignedCaWorkflowRequest(issuerKey.Value!.PrivateKeyId, "Root CA", "CN=Root CA", 3650),
            CancellationToken.None);
        var intermediateKey = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Intermediate Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var csr = await service.CreateCertificateSigningRequestAsync(
            new CreateCertificateSigningRequestWorkflowRequest(
                intermediateKey.Value!.PrivateKeyId,
                string.Empty,
                string.Empty,
                [],
                false,
                null,
                [],
                [],
                template.Value!.TemplateId),
            CancellationToken.None);
        var issued = await service.SignCertificateSigningRequestAsync(
            new SignStoredCertificateSigningRequestRequest(
                csr.Value!.CertificateSigningRequestId,
                issuerCertificate.Value!.CertificateId,
                issuerKey.Value.PrivateKeyId,
                string.Empty,
                0,
                template.Value.TemplateId),
            CancellationToken.None);

        Assert.True(csr.IsSuccess);
        var csrValue = csr.Value;
        Assert.NotNull(csrValue);
        Assert.True(csrValue!.Details.IsCertificateAuthority);
        Assert.Contains("KeyCertSign", csrValue.Details.KeyUsages!);
        Assert.True(issued.IsSuccess);
        Assert.True(issued.Value!.Details.IsCertificateAuthority);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddManagedCryptoServices();
        services.AddApplication(new ConfigurationBuilder().Build());
        return services.BuildServiceProvider();
    }

    private static string GetDatabasePath() => Path.Combine(Path.GetTempPath(), $"xcanet-int-m10-{Guid.NewGuid():N}.db");
}
