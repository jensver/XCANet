using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using XcaNet.App.Services;
using XcaNet.App.Commands;
using XcaNet.App.ViewModels;
using XcaNet.Application.DependencyInjection;
using XcaNet.Application.Services;
using XcaNet.Contracts.Crypto;
using XcaNet.Contracts.Crypto.Workflow;
using XcaNet.Contracts.Database;
using XcaNet.Crypto.DotNet.DependencyInjection;

namespace XcaNet.Integration.Tests;

public sealed class ShellWorkflowTests
{
    [Fact]
    public async Task RevokeAndCrlCommands_ShouldEnableWhenCertificateSelectionSupportsThem()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "Shell Test"), CancellationToken.None);
        await service.OpenDatabaseAsync(new OpenDatabaseRequest(databasePath), CancellationToken.None);
        await service.UnlockDatabaseAsync(new UnlockDatabaseRequest("correct horse battery staple"), CancellationToken.None);

        var issuerKey = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Issuer Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var issuerCertificate = await service.CreateSelfSignedCaAsync(new CreateSelfSignedCaWorkflowRequest(issuerKey.Value!.PrivateKeyId, "Issuer CA", "CN=Issuer CA", 365), CancellationToken.None);
        var leafKey = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Leaf Key", KeyAlgorithmKind.Ecdsa, null, EllipticCurveKind.P256), CancellationToken.None);
        var csr = await service.CreateCertificateSigningRequestAsync(
            new CreateCertificateSigningRequestWorkflowRequest(leafKey.Value!.PrivateKeyId, "Leaf CSR", "CN=leaf.example.test", [new SanEntry("leaf.example.test")]),
            CancellationToken.None);
        var leafCertificate = await service.SignCertificateSigningRequestAsync(
            new SignStoredCertificateSigningRequestRequest(csr.Value!.CertificateSigningRequestId, issuerCertificate.Value!.CertificateId, issuerKey.Value.PrivateKeyId, "Leaf Certificate", 180),
            CancellationToken.None);

        var shell = new ShellViewModel(service, new TestDesktopFileDialogService(), NullLogger<ShellViewModel>.Instance);
        await ((AsyncCommand)shell.CertificatesPage.RefreshCommand!).ExecuteAsync();
        shell.CertificatesPage.SelectedItem = shell.CertificatesPage.Items.Single(x => x.CertificateId == leafCertificate.Value!.CertificateId);
        await Task.Delay(50);

        Assert.False(shell.CertificatesPage.GenerateCertificateRevocationListCommand!.CanExecute(null));
        Assert.False(shell.CertificatesPage.RevokeSelectedCommand!.CanExecute(null));

        shell.CertificatesPage.RevocationConfirmationText = "REVOKE";
        Assert.True(shell.CertificatesPage.RevokeSelectedCommand.CanExecute(null));

        await shell.CertificatesPage.RevokeSelectedCommand.As<AsyncCommand>().ExecuteAsync();
        await ((AsyncCommand)shell.CertificatesPage.RefreshCommand!).ExecuteAsync();

        Assert.Equal("Revoked", shell.CertificatesPage.SelectedItem!.RevocationStatus);
        shell.CertificatesPage.SelectedItem = shell.CertificatesPage.Items.Single(x => x.CertificateId == issuerCertificate.Value.CertificateId);
        await Task.Delay(50);
        Assert.True(shell.CertificatesPage.GenerateCertificateRevocationListCommand.CanExecute(null));
        await shell.CertificatesPage.GenerateCertificateRevocationListCommand.As<AsyncCommand>().ExecuteAsync();
        await ((AsyncCommand)shell.CertificateRevocationListsPage.RefreshCommand!).ExecuteAsync();

        Assert.NotEmpty(shell.CertificateRevocationListsPage.Items);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddManagedCryptoServices();
        services.AddApplication(new ConfigurationBuilder().Build());
        return services.BuildServiceProvider();
    }

    private static string GetDatabasePath() => Path.Combine(Path.GetTempPath(), $"xcanet-shell-m5-{Guid.NewGuid():N}.db");
}

file static class CommandCastingExtensions
{
    public static T As<T>(this object command)
        where T : class
    {
        return (T)command;
    }
}

file sealed class TestDesktopFileDialogService : IDesktopFileDialogService
{
    public void SetOwner(Avalonia.Controls.Window? window)
    {
    }

    public Task<IReadOnlyList<string>> PickImportFilesAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<string>>([]);

    public Task<string?> PickSavePathAsync(string suggestedFileName, CancellationToken cancellationToken)
        => Task.FromResult<string?>(null);
}
