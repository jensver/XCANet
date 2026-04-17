using XcaNet.App.ViewModels.Items;
using XcaNet.App.ViewModels.Pages;
using XcaNet.Contracts.Browser;
using XcaNet.Contracts.Crypto;

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
    public void CertificateInspectorViewModel_Apply_ShouldExposeRelationshipsAndDetails()
    {
        var inspector = new CertificateInspectorViewModel();
        var child = new RelatedCertificateSummary(Guid.NewGuid(), "Issued Leaf", "CN=leaf.example.test");
        var certificate = new CertificateInspector(
            Guid.NewGuid(),
            "Root CA",
            new CertificateDetails(
                "CN=Root CA",
                "CN=Root CA",
                "01",
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(30),
                "SHA1",
                "SHA256",
                "RSA",
                true,
                ["KeyCertSign", "CrlSign"],
                ["Server Authentication"],
                ["root.example.test"]),
            "Active",
            Guid.NewGuid(),
            "Issuer CA",
            Guid.NewGuid(),
            "Root Key",
            [child]);

        inspector.Apply(certificate);

        Assert.True(inspector.HasCertificate);
        Assert.True(inspector.CanOpenIssuer);
        Assert.True(inspector.CanOpenPrivateKey);
        Assert.True(inspector.CanOpenSelectedChild);
        Assert.Equal("Root CA", inspector.DisplayName);
        Assert.Equal("Certificate Authority", inspector.CertificateAuthorityStatus);
        Assert.Equal("Root Key", inspector.PrivateKeyDisplayName);
        Assert.Single(inspector.ChildCertificates);
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
            0);
    }
}
