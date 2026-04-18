using XcaNet.App.ViewModels.Pages;
using XcaNet.Contracts.Browser;
using XcaNet.Contracts.Crypto;
using XcaNet.Contracts.Revocation;

namespace XcaNet.Integration.Tests;

public sealed class PageViewModelTests
{
    [Fact]
    public void CertificatesPageViewModel_SetItems_ShouldRetainSelectionWhenPossible()
    {
        var page = new CertificatesPageViewModel();
        var certificateA = CreateCertificateListItem(Guid.NewGuid(), "Alpha");
        var certificateB = CreateCertificateListItem(Guid.NewGuid(), "Beta");

        page.SetItems([certificateA, certificateB]);
        page.SelectedItem = certificateB;

        page.SetItems([certificateA, certificateB with { DisplayName = "Beta Updated" }]);

        Assert.NotNull(page.SelectedItem);
        Assert.Equal(certificateB.CertificateId, page.SelectedItem!.CertificateId);
        Assert.Equal("Beta Updated", page.SelectedItem.DisplayName);
    }

    [Fact]
    public void CertificateInspectorData_ShouldExposeRelationshipsAndDetails()
    {
        var childTarget = new NavigationTarget(BrowserEntityType.Certificate, Guid.NewGuid(), NavigationFocusSection.Inspector);
        var certificate = new CertificateInspectorData(
            Guid.NewGuid(),
            new CertificateDisplayFields("Root CA", "range", "Certificate Authority", "Issuer CA", "Root Key"),
            new CertificateRawFields(
                "CN=Root CA",
                "CN=Root CA",
                "01",
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(30),
                "SHA1",
                "SHA256",
                "RSA"),
            new CertificateExtensionFields(
                true,
                ["root.example.test"],
                ["KeyCertSign", "CrlSign"],
                ["Server Authentication"]),
            new CertificateRevocationInfo(false, "Active", null, null, null),
            new CertificateNavigationInfo(
                new NavigationTarget(BrowserEntityType.Certificate, Guid.NewGuid(), NavigationFocusSection.Inspector),
                new NavigationTarget(BrowserEntityType.PrivateKey, Guid.NewGuid(), NavigationFocusSection.Overview),
                [new RelatedNavigationItem("Issued Leaf", "CN=leaf.example.test", childTarget)]));

        Assert.Equal("Root CA", certificate.Display.DisplayName);
        Assert.Equal("Certificate Authority", certificate.Display.CertificateKind);
        Assert.Equal("Root Key", certificate.Display.PrivateKeyDisplayName);
        Assert.True(certificate.Extensions.IsCertificateAuthority);
        Assert.NotNull(certificate.Navigation.Issuer);
        Assert.NotNull(certificate.Navigation.PrivateKey);
        Assert.Single(certificate.Navigation.Children);
    }

    [Fact]
    public void CertificateRequestsPageViewModel_SetIssuers_ShouldOnlyIncludeCertificateAuthorities()
    {
        var page = new CertificateRequestsPageViewModel();
        var issuer = CreateCertificateListItem(Guid.NewGuid(), "Issuer", true);
        var leaf = CreateCertificateListItem(Guid.NewGuid(), "Leaf", false);
        var key = new PrivateKeyListItem(Guid.NewGuid(), "Issuer Key", "RSA", "fp", DateTimeOffset.UtcNow, 1);

        page.SetIssuers([issuer, leaf], [key]);

        Assert.Single(page.IssuerCertificates);
        Assert.Equal(issuer.CertificateId, page.IssuerCertificates[0].CertificateId);
        Assert.Single(page.IssuerPrivateKeys);
        Assert.Equal(key.PrivateKeyId, page.IssuerPrivateKeys[0].PrivateKeyId);
    }

    [Fact]
    public void ItemsPages_ShouldExposeEmptyStateFlags()
    {
        var page = new CertificatesPageViewModel();

        Assert.True(page.IsEmpty);
        Assert.False(page.HasItems);

        page.SetItems([CreateCertificateListItem(Guid.NewGuid(), "Alpha")]);

        Assert.False(page.IsEmpty);
        Assert.True(page.HasItems);
    }

    private static CertificateListItem CreateCertificateListItem(Guid id, string displayName, bool isCertificateAuthority = false)
    {
        return new CertificateListItem(
            id,
            displayName,
            $"CN={displayName}",
            "CN=Issuer",
            "01",
            "sha1",
            "sha256",
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30),
            "RSA",
            isCertificateAuthority,
            "Active",
            null,
            null,
            null,
            null,
            0);
    }
}
