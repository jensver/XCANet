using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using XcaNet.App.Services;
using XcaNet.App.Commands;
using XcaNet.App.ViewModels;
using XcaNet.App.ViewModels.Pages;
using XcaNet.Application.DependencyInjection;
using XcaNet.Application.Services;
using XcaNet.Contracts.Browser;
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

    [Fact]
    public async Task CentralAuthoringSurface_ShouldSupportTemplateModesAndCoreCreationFlow()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "M13 Test"), CancellationToken.None);

        await service.OpenDatabaseAsync(new OpenDatabaseRequest(databasePath), CancellationToken.None);
        await service.UnlockDatabaseAsync(new UnlockDatabaseRequest("correct horse battery staple"), CancellationToken.None);

        var shell = new ShellViewModel(service, new TestDesktopFileDialogService(), NullLogger<ShellViewModel>.Instance);
        while (shell.IsBusy)
        {
            await Task.Delay(20);
        }

        await ((AsyncCommand)shell.PrivateKeysPage.RefreshCommand!).ExecuteAsync();
        await ((AsyncCommand)shell.CertificateRequestsPage.RefreshCommand!).ExecuteAsync();
        await ((AsyncCommand)shell.TemplatesPage.RefreshCommand!).ExecuteAsync();

        shell.PrivateKeysPage.NewKeyDisplayName = "Root Key";
        await shell.PrivateKeysPage.GenerateKeyCommand!.As<AsyncCommand>().ExecuteAsync();
        shell.PrivateKeysPage.SelectedItem = shell.PrivateKeysPage.Items.Single(x => x.DisplayName == "Root Key");
        shell.PrivateKeysPage.OpenSelfSignedCaAuthoringCommand!.As<DelegateCommand>().Execute(null);
        Assert.True(shell.IsAuthoringDialogOpen);
        Assert.True(shell.IsCertificateAuthoringDialogOpen);
        shell.PrivateKeysPage.SelfSignedCaAuthoring.DisplayName = "Root CA";
        shell.PrivateKeysPage.SelfSignedCaAuthoring.SubjectName = "CN=Root CA";
        shell.PrivateKeysPage.SelfSignedCaAuthoring.ValidityDays = 3650;
        await shell.PrivateKeysPage.CreateSelfSignedCaCommand!.As<AsyncCommand>().ExecuteAsync();
        Assert.False(shell.IsAuthoringDialogOpen);

        shell.PrivateKeysPage.NewKeyDisplayName = "Leaf Key";
        shell.PrivateKeysPage.SelectedAlgorithm = KeyAlgorithmView.Ecdsa;
        shell.PrivateKeysPage.SelectedCurve = EllipticCurveView.P256;
        await shell.PrivateKeysPage.GenerateKeyCommand!.As<AsyncCommand>().ExecuteAsync();
        shell.PrivateKeysPage.SelectedItem = shell.PrivateKeysPage.Items.Single(x => x.DisplayName == "Leaf Key");
        shell.PrivateKeysPage.OpenCertificateSigningRequestAuthoringCommand!.As<DelegateCommand>().Execute(null);
        Assert.True(shell.IsAuthoringDialogOpen);
        shell.PrivateKeysPage.CertificateSigningRequestAuthoring.DisplayName = "Leaf CSR";
        shell.PrivateKeysPage.CertificateSigningRequestAuthoring.SubjectName = "CN=leaf.example.test";
        shell.PrivateKeysPage.CertificateSigningRequestAuthoring.SubjectAlternativeNames = "leaf.example.test, api.example.test";
        shell.PrivateKeysPage.CertificateSigningRequestAuthoring.KeyUsages = "DigitalSignature, KeyEncipherment";
        shell.PrivateKeysPage.CertificateSigningRequestAuthoring.EnhancedKeyUsages = "Server Authentication";

        await shell.PrivateKeysPage.CreateCertificateSigningRequestCommand!.As<AsyncCommand>().ExecuteAsync();
        Assert.False(shell.IsAuthoringDialogOpen);
        await shell.CertificateRequestsPage.RefreshCommand!.As<AsyncCommand>().ExecuteAsync();

        shell.CertificateRequestsPage.SelectedItem = shell.CertificateRequestsPage.Items.Single(x => x.DisplayName == "Leaf CSR");
        shell.CertificateRequestsPage.OpenIssuanceAuthoringCommand!.As<DelegateCommand>().Execute(null);
        Assert.True(shell.IsAuthoringDialogOpen);
        shell.CertificateRequestsPage.IssuanceAuthoring.DisplayName = "Issued Leaf";
        shell.CertificateRequestsPage.IssuanceAuthoring.SelectedIssuerCertificate = shell.CertificateRequestsPage.IssuanceAuthoring.IssuerCertificates.Single(x => x.DisplayName == "Root CA");
        shell.CertificateRequestsPage.IssuanceAuthoring.SelectedIssuerPrivateKey = shell.CertificateRequestsPage.IssuanceAuthoring.IssuerPrivateKeys.Single(x => x.DisplayName == "Root Key");
        await shell.CertificateRequestsPage.SignSelectedCommand!.As<AsyncCommand>().ExecuteAsync();
        Assert.False(shell.IsAuthoringDialogOpen);
        await shell.CertificatesPage.RefreshCommand!.As<AsyncCommand>().ExecuteAsync();

        var issuedCertificate = shell.CertificatesPage.Items.Single(x => x.DisplayName == "Issued Leaf");
        Assert.Equal("CN=Root CA", issuedCertificate.Issuer);

        shell.CertificatesPage.SelectedItem = issuedCertificate;
        await Task.Delay(50);
        shell.CertificatesPage.CreateTemplateFromCertificateCommand!.As<DelegateCommand>().Execute(null);

        Assert.True(shell.IsAuthoringDialogOpen);
        Assert.True(shell.IsTemplateAuthoringDialogOpen);
        Assert.Equal("Issued Leaf derived template", shell.TemplatesPage.Name);
        Assert.Equal(TemplateIntendedUsage.EndEntityCertificate, shell.TemplatesPage.IntendedUsage);

        shell.CertificateRequestsPage.SelectedItem = shell.CertificateRequestsPage.Items.Single(x => x.DisplayName == "Leaf CSR");
        shell.CertificateRequestsPage.CreateTemplateFromRequestCommand!.As<DelegateCommand>().Execute(null);

        Assert.Equal("Leaf CSR derived template", shell.TemplatesPage.Name);
        Assert.Equal(TemplateIntendedUsage.CertificateSigningRequest, shell.TemplatesPage.IntendedUsage);

        shell.CertificateRequestsPage.CreateSimilarRequestCommand!.As<DelegateCommand>().Execute(null);

        Assert.True(shell.IsAuthoringDialogOpen);
        Assert.True(shell.IsCertificateAuthoringDialogOpen);
        Assert.Equal("Leaf CSR Copy", shell.PrivateKeysPage.CertificateSigningRequestAuthoring.DisplayName);
        Assert.Equal("CN=leaf.example.test", shell.PrivateKeysPage.CertificateSigningRequestAuthoring.SubjectName);

        shell.CloseAuthoringDialogCommand.As<DelegateCommand>().Execute(null);
        Assert.False(shell.IsAuthoringDialogOpen);
    }

    [Fact]
    public void Shell_ShouldDefaultToObjectWorkspaceTabs()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var shell = new ShellViewModel(service, new TestDesktopFileDialogService(), NullLogger<ShellViewModel>.Instance);

        Assert.Equal("Certificates", shell.CurrentPage.Title);
        Assert.Equal(5, shell.WorkspaceNavigationItems.Count);
        Assert.Equal("Private Keys", shell.WorkspaceNavigationItems[0].Title);
        Assert.Equal("CRLs", shell.WorkspaceNavigationItems[^1].Title);
        Assert.Equal(2, shell.UtilityNavigationItems.Count);
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
