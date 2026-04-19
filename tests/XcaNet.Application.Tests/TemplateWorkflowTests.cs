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

public sealed class TemplateWorkflowTests
{
    [Fact]
    public async Task SaveCloneAndApplyTemplateAsync_ShouldPersistAndReturnDefaults()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "Template Test"), CancellationToken.None);

        var saved = await service.SaveTemplateAsync(
            new SaveTemplateRequest(
                null,
                "Leaf TLS",
                "Leaf issuance defaults",
                false,
                true,
                TemplateIntendedUsage.EndEntityCertificate,
                "CN=service.example.test",
                ["service.example.test", "api.example.test"],
                KeyAlgorithmKind.Ecdsa,
                null,
                EllipticCurveKind.P256,
                "SHA-256",
                397,
                false,
                null,
                ["DigitalSignature", "KeyEncipherment"],
                ["Server Authentication", "Client Authentication"]),
            CancellationToken.None);

        var applied = await service.ApplyTemplateAsync(
            new ApplyTemplateRequest(saved.Value!.TemplateId, TemplateWorkflowKind.CertificateSigningRequest),
            CancellationToken.None);
        var cloned = await service.CloneTemplateAsync(new CloneTemplateRequest(saved.Value.TemplateId, "Leaf TLS Copy"), CancellationToken.None);
        var listed = await service.ListTemplatesAsync(CancellationToken.None);

        Assert.True(saved.IsSuccess);
        Assert.True(applied.IsSuccess);
        Assert.Equal("CN=service.example.test", applied.Value!.SubjectDefault);
        Assert.Contains("api.example.test", applied.Value.SubjectAlternativeNames);
        Assert.True(cloned.IsSuccess);
        Assert.True(listed.IsSuccess);
        Assert.Equal(2, listed.Value!.Count);
    }

    [Fact]
    public async Task SaveTemplateAsync_WithInvalidCaPolicy_ShouldFail()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "Template Validation"), CancellationToken.None);

        var result = await service.SaveTemplateAsync(
            new SaveTemplateRequest(
                null,
                "Broken CA",
                null,
                false,
                true,
                TemplateIntendedUsage.SelfSignedCa,
                "CN=Broken CA",
                [],
                KeyAlgorithmKind.Rsa,
                3072,
                null,
                "SHA-256",
                3650,
                false,
                null,
                ["DigitalSignature"],
                []),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("CA templates", result.Message);
    }

    [Fact]
    public async Task DisabledTemplate_ShouldBeRejectedForWorkflowUse()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "Template Disabled"), CancellationToken.None);
        var saved = await service.SaveTemplateAsync(
            new SaveTemplateRequest(
                null,
                "Disabled Leaf",
                null,
                false,
                false,
                TemplateIntendedUsage.EndEntityCertificate,
                "CN=disabled.example.test",
                [],
                KeyAlgorithmKind.Rsa,
                3072,
                null,
                "SHA-256",
                365,
                false,
                null,
                ["DigitalSignature"],
                ["Server Authentication"]),
            CancellationToken.None);

        var applied = await service.ApplyTemplateAsync(
            new ApplyTemplateRequest(saved.Value!.TemplateId, TemplateWorkflowKind.CertificateSigningRequest),
            CancellationToken.None);

        Assert.False(applied.IsSuccess);
        Assert.Contains("Disabled templates", applied.Message);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddManagedCryptoServices();
        services.AddApplication(new ConfigurationBuilder().Build());
        return services.BuildServiceProvider();
    }

    private static string GetDatabasePath() => Path.Combine(Path.GetTempPath(), $"xcanet-app-m10-{Guid.NewGuid():N}.db");
}
