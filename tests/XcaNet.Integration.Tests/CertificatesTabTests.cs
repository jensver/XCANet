using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using XcaNet.App.Commands;
using XcaNet.App.Services;
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

public sealed class CertificatesTabTests
{
    // ---------------------------------------------------------------------------
    // CertificateListItem computed properties
    // ---------------------------------------------------------------------------

    [Theory]
    [InlineData("CN=Root CA, O=Test", "Root CA")]
    [InlineData("O=Test, CN=leaf.example.test", "leaf.example.test")]
    [InlineData("O=NoCn", "O=NoCn")]
    [InlineData("cn=case.insensitive", "case.insensitive")]
    public void CertificateListItem_CommonName_ShouldExtractCnPart(string subject, string expected)
    {
        var item = MakeCertItem(subject: subject);
        Assert.Equal(expected, item.CommonName);
    }

    [Fact]
    public void CertificateListItem_StatusDisplay_ShouldReturnRevoked_WhenRevocationStatusIsRevoked()
    {
        var item = MakeCertItem(revocationStatus: "Revoked");
        Assert.Equal("Revoked", item.StatusDisplay);
    }

    [Fact]
    public void CertificateListItem_StatusDisplay_ShouldReturnExpired_WhenNotAfterInPast()
    {
        var item = MakeCertItem(notBefore: DateTimeOffset.UtcNow.AddDays(-365), notAfter: DateTimeOffset.UtcNow.AddDays(-1));
        Assert.Equal("Expired", item.StatusDisplay);
    }

    [Fact]
    public void CertificateListItem_StatusDisplay_ShouldReturnNotYetValid_WhenNotBeforeInFuture()
    {
        var item = MakeCertItem(notBefore: DateTimeOffset.UtcNow.AddDays(1), notAfter: DateTimeOffset.UtcNow.AddDays(365));
        Assert.Equal("Not yet valid", item.StatusDisplay);
    }

    [Fact]
    public void CertificateListItem_StatusDisplay_ShouldReturnValid_WhenWithinValidity()
    {
        var item = MakeCertItem(notBefore: DateTimeOffset.UtcNow.AddDays(-1), notAfter: DateTimeOffset.UtcNow.AddDays(365));
        Assert.Equal("Valid", item.StatusDisplay);
    }

    // ---------------------------------------------------------------------------
    // CertificatesPageViewModel.RebuildTree
    // ---------------------------------------------------------------------------

    [Fact]
    public void RebuildTree_ShouldPlaceSelfSignedCertificatesAtRoot()
    {
        var vm = new CertificatesPageViewModel();
        var rootId = Guid.NewGuid();
        var root = MakeCertItem(id: rootId, issuerCertificateId: rootId, isCa: true); // self-signed

        vm.RebuildTree([root]);

        Assert.Single(vm.CertificateTreeRoots);
        Assert.Equal(rootId, vm.CertificateTreeRoots[0].CertificateId);
        Assert.Empty(vm.CertificateTreeRoots[0].Children);
    }

    [Fact]
    public void RebuildTree_ShouldNestLeafUnderIssuer()
    {
        var vm = new CertificatesPageViewModel();
        var rootId = Guid.NewGuid();
        var leafId = Guid.NewGuid();
        var root = MakeCertItem(id: rootId, issuerCertificateId: rootId, isCa: true);
        var leaf = MakeCertItem(id: leafId, issuerCertificateId: rootId);

        vm.RebuildTree([root, leaf]);

        Assert.Single(vm.CertificateTreeRoots);
        var rootNode = vm.CertificateTreeRoots[0];
        Assert.Single(rootNode.Children);
        Assert.Equal(leafId, rootNode.Children[0].CertificateId);
    }

    [Fact]
    public void RebuildTree_ShouldPlaceOrphanCertificatesAtRoot_WhenIssuerNotInSet()
    {
        var vm = new CertificatesPageViewModel();
        var unknownIssuerId = Guid.NewGuid();
        var leafId = Guid.NewGuid();
        var leaf = MakeCertItem(id: leafId, issuerCertificateId: unknownIssuerId);

        vm.RebuildTree([leaf]);

        Assert.Single(vm.CertificateTreeRoots);
        Assert.Equal(leafId, vm.CertificateTreeRoots[0].CertificateId);
    }

    [Fact]
    public void RebuildTree_ShouldHandleNullIssuerCertificateId_AsRoot()
    {
        var vm = new CertificatesPageViewModel();
        var id = Guid.NewGuid();
        var cert = MakeCertItem(id: id, issuerCertificateId: null);

        vm.RebuildTree([cert]);

        Assert.Single(vm.CertificateTreeRoots);
        Assert.Equal(id, vm.CertificateTreeRoots[0].CertificateId);
    }

    [Fact]
    public void RebuildTree_ShouldClearExistingTreeBeforeRebuilding()
    {
        var vm = new CertificatesPageViewModel();
        var id1 = Guid.NewGuid();
        vm.RebuildTree([MakeCertItem(id: id1)]);
        Assert.Single(vm.CertificateTreeRoots);

        vm.RebuildTree([]);
        Assert.Empty(vm.CertificateTreeRoots);
    }

    [Fact]
    public void RebuildTree_ShouldSupportMultiLevelHierarchy()
    {
        var vm = new CertificatesPageViewModel();
        var rootId = Guid.NewGuid();
        var intId = Guid.NewGuid();
        var leafId = Guid.NewGuid();

        var root = MakeCertItem(id: rootId, issuerCertificateId: rootId, isCa: true);
        var intermediate = MakeCertItem(id: intId, issuerCertificateId: rootId, isCa: true);
        var leaf = MakeCertItem(id: leafId, issuerCertificateId: intId);

        vm.RebuildTree([root, intermediate, leaf]);

        Assert.Single(vm.CertificateTreeRoots);
        var rootNode = vm.CertificateTreeRoots[0];
        Assert.Single(rootNode.Children);
        var intNode = rootNode.Children[0];
        Assert.Equal(intId, intNode.CertificateId);
        Assert.Single(intNode.Children);
        Assert.Equal(leafId, intNode.Children[0].CertificateId);
    }

    // ---------------------------------------------------------------------------
    // Dialog state commands
    // ---------------------------------------------------------------------------

    [Fact]
    public void IsPlainView_ShouldDefaultToFalse()
    {
        var vm = new CertificatesPageViewModel();
        Assert.False(vm.IsPlainView);
    }

    [Fact]
    public void IsDetailDialogOpen_ShouldDefaultToFalse()
    {
        var vm = new CertificatesPageViewModel();
        Assert.False(vm.IsDetailDialogOpen);
    }

    [Fact]
    public void IsRevokeDialogOpen_ShouldDefaultToFalse()
    {
        var vm = new CertificatesPageViewModel();
        Assert.False(vm.IsRevokeDialogOpen);
    }

    // ---------------------------------------------------------------------------
    // Shell command wiring — revoke dialog flow
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task RevokeDialogFlow_ShouldOpenAndGateRevokeCommand()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "M13.6 Test"), CancellationToken.None);
        await service.OpenDatabaseAsync(new OpenDatabaseRequest(databasePath), CancellationToken.None);
        await service.UnlockDatabaseAsync(new UnlockDatabaseRequest("correct horse battery staple"), CancellationToken.None);

        var issuerKey = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("CA Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var issuerCert = await service.CreateSelfSignedCaAsync(new CreateSelfSignedCaWorkflowRequest(issuerKey.Value!.PrivateKeyId, "CA", "CN=CA", 365), CancellationToken.None);
        var leafKey = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Leaf Key", KeyAlgorithmKind.Ecdsa, null, EllipticCurveKind.P256), CancellationToken.None);
        var csr = await service.CreateCertificateSigningRequestAsync(
            new CreateCertificateSigningRequestWorkflowRequest(leafKey.Value!.PrivateKeyId, "Leaf CSR", "CN=leaf", []),
            CancellationToken.None);
        var leafCert = await service.SignCertificateSigningRequestAsync(
            new SignStoredCertificateSigningRequestRequest(csr.Value!.CertificateSigningRequestId, issuerCert.Value!.CertificateId, issuerKey.Value.PrivateKeyId, "Leaf", 90),
            CancellationToken.None);

        var shell = new ShellViewModel(service, new TestFileDialogService(), NullLogger<ShellViewModel>.Instance);
        await ((AsyncCommand)shell.CertificatesPage.RefreshCommand!).ExecuteAsync();
        shell.CertificatesPage.SelectedItem = shell.CertificatesPage.Items.Single(x => x.CertificateId == leafCert.Value!.CertificateId);
        await Task.Delay(50);

        // Revoke command disabled before dialog open
        Assert.False(shell.CertificatesPage.RevokeSelectedCommand!.CanExecute(null));

        // Open revoke dialog
        shell.CertificatesPage.OpenRevokeDialogCommand!.Execute(null);
        Assert.True(shell.CertificatesPage.IsRevokeDialogOpen);
        Assert.True(shell.CertificatesPage.RevokeSelectedCommand.CanExecute(null));

        // Close dialog via cancel
        shell.CertificatesPage.CloseRevokeDialogCommand!.Execute(null);
        Assert.False(shell.CertificatesPage.IsRevokeDialogOpen);
        Assert.False(shell.CertificatesPage.RevokeSelectedCommand.CanExecute(null));
    }

    [Fact]
    public async Task TogglePlainViewCommand_ShouldToggleIsPlainView()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var shell = new ShellViewModel(service, new TestFileDialogService(), NullLogger<ShellViewModel>.Instance);

        Assert.False(shell.CertificatesPage.IsPlainView);
        shell.CertificatesPage.TogglePlainViewCommand!.Execute(null);
        Assert.True(shell.CertificatesPage.IsPlainView);
        shell.CertificatesPage.TogglePlainViewCommand.Execute(null);
        Assert.False(shell.CertificatesPage.IsPlainView);
    }

    [Fact]
    public async Task TreeRebuild_ShouldReflectIssuerHierarchyAfterRefresh()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "Tree Test"), CancellationToken.None);
        await service.OpenDatabaseAsync(new OpenDatabaseRequest(databasePath), CancellationToken.None);
        await service.UnlockDatabaseAsync(new UnlockDatabaseRequest("correct horse battery staple"), CancellationToken.None);

        var caKey = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("CA Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var caCert = await service.CreateSelfSignedCaAsync(new CreateSelfSignedCaWorkflowRequest(caKey.Value!.PrivateKeyId, "Root CA", "CN=Root CA", 365), CancellationToken.None);
        var leafKey = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Leaf Key", KeyAlgorithmKind.Ecdsa, null, EllipticCurveKind.P256), CancellationToken.None);
        var csr = await service.CreateCertificateSigningRequestAsync(
            new CreateCertificateSigningRequestWorkflowRequest(leafKey.Value!.PrivateKeyId, "Leaf CSR", "CN=leaf", []),
            CancellationToken.None);
        await service.SignCertificateSigningRequestAsync(
            new SignStoredCertificateSigningRequestRequest(csr.Value!.CertificateSigningRequestId, caCert.Value!.CertificateId, caKey.Value.PrivateKeyId, "Leaf", 90),
            CancellationToken.None);

        var shell = new ShellViewModel(service, new TestFileDialogService(), NullLogger<ShellViewModel>.Instance);
        await ((AsyncCommand)shell.CertificatesPage.RefreshCommand!).ExecuteAsync();

        // In tree view: exactly 1 root (the CA) with 1 child (leaf)
        Assert.Single(shell.CertificatesPage.CertificateTreeRoots);
        var caNode = shell.CertificatesPage.CertificateTreeRoots[0];
        Assert.True(caNode.IsCertificateAuthority);
        Assert.Single(caNode.Children);
        Assert.False(caNode.Children[0].IsCertificateAuthority);
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static CertificateListItem MakeCertItem(
        Guid? id = null,
        string displayName = "Test",
        string subject = "CN=Test",
        string issuer = "CN=Test",
        string serialNumber = "00",
        string sha1 = "AA",
        string sha256 = "BB",
        DateTimeOffset? notBefore = null,
        DateTimeOffset? notAfter = null,
        string keyAlgorithm = "RSA",
        bool isCa = false,
        string revocationStatus = "Good",
        string? revocationReason = null,
        DateTimeOffset? revokedAt = null,
        Guid? issuerCertificateId = null,
        Guid? privateKeyId = null,
        int childCount = 0)
    {
        return new CertificateListItem(
            id ?? Guid.NewGuid(),
            displayName,
            subject,
            issuer,
            serialNumber,
            sha1,
            sha256,
            notBefore ?? DateTimeOffset.UtcNow.AddDays(-1),
            notAfter ?? DateTimeOffset.UtcNow.AddDays(365),
            keyAlgorithm,
            isCa,
            revocationStatus,
            revocationReason,
            revokedAt,
            issuerCertificateId,
            privateKeyId,
            childCount);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddManagedCryptoServices();
        services.AddApplication(new ConfigurationBuilder().Build());
        return services.BuildServiceProvider();
    }

    private static string GetDatabasePath()
        => Path.Combine(Path.GetTempPath(), $"xcanet-certs-m13-6-{Guid.NewGuid():N}.db");
}

file sealed class TestFileDialogService : IDesktopFileDialogService
{
    public void SetOwner(Avalonia.Controls.Window? window) { }
    public Task<IReadOnlyList<string>> PickImportFilesAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<string>>([]);
    public Task<string?> PickSavePathAsync(string suggestedFileName, CancellationToken cancellationToken)
        => Task.FromResult<string?>(null);
}
