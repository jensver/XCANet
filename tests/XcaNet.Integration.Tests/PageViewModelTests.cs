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

    [Fact]
    public void TemplatesPageViewModel_ShouldFilterAndBuildSaveRequest()
    {
        var page = new TemplatesPageViewModel();
        var enabled = new TemplateListItem(Guid.NewGuid(), "Leaf", null, TemplateIntendedUsage.EndEntityCertificate, false, true, "RSA 3072 | 365 day(s)");
        var disabled = new TemplateListItem(Guid.NewGuid(), "Disabled", null, TemplateIntendedUsage.SelfSignedCa, false, false, "RSA 3072 | 3650 day(s)");

        page.SetTemplates([enabled, disabled]);
        page.StatusFilter = TemplateStatusFilterView.All;
        page.UsageFilter = TemplateUsageFilterView.EndEntityCertificate;
        page.SelectedItem = enabled;
        page.LoadTemplate(
            new TemplateDetails(
                enabled.TemplateId,
                enabled.Name,
                null,
                false,
                true,
                TemplateIntendedUsage.EndEntityCertificate,
                "CN=leaf.example.test",
                ["leaf.example.test"],
                KeyAlgorithmKind.Ecdsa,
                null,
                EllipticCurveKind.P256,
                "SHA-256",
                180,
                false,
                null,
                ["DigitalSignature", "KeyEncipherment"],
                ["Server Authentication"],
                new TemplatePreviewSummary("EndEntityCertificate", "CN=leaf.example.test", "leaf.example.test", "ECDSA P256", "180 day(s)", "Leaf", "Enabled"),
                new TemplateValidationSummary([], [])));

        var saveRequest = page.BuildSaveRequest();

        Assert.Single(page.Items);
        Assert.Equal(enabled.TemplateId, page.SelectedItem!.TemplateId);
        Assert.Equal("CN=leaf.example.test", saveRequest.SubjectDefault);
        Assert.Equal(KeyAlgorithmKind.Ecdsa, saveRequest.KeyAlgorithm);
        Assert.Contains("Server Authentication", saveRequest.EnhancedKeyUsages);
    }

    [Fact]
    public void CertificateAuthoringViewModel_ApplyTemplateDefaults_ShouldRespectMode()
    {
        var page = new CertificateAuthoringViewModel(
            "CSR Authoring",
            "Operation: create request",
            "Source: selected key",
            "Original Display",
            "CN=original.example.test",
            180,
            false,
            "DigitalSignature",
            "Server Authentication",
            true,
            true,
            false,
            true,
            true,
            "Create CSR");
        var defaults = new AppliedTemplateDefaults(
            Guid.NewGuid(),
            "Template Display",
            TemplateWorkflowKind.CertificateSigningRequest,
            "CN=template.example.test",
            ["template.example.test", "api.template.example.test"],
            KeyAlgorithmKind.Ecdsa,
            null,
            EllipticCurveKind.P384,
            "SHA-384",
            397,
            true,
            2,
            ["DigitalSignature", "KeyEncipherment"],
            ["Client Authentication"],
            new TemplatePreviewSummary("usage", "subject", "san", "key", "validity", "extensions", "state"),
            new TemplateValidationSummary([], []));

        page.SelectedTemplateApplicationMode = TemplateApplicationModeView.SubjectOnly;
        page.ApplyTemplateDefaults(defaults);

        Assert.Equal("Original Display", page.DisplayName);
        Assert.Equal("CN=template.example.test", page.SubjectName);
        Assert.Equal("template.example.test, api.template.example.test", page.SubjectAlternativeNames);
        Assert.Equal(180, page.ValidityDays);
        Assert.False(page.IsCertificateAuthority);

        page.SelectedTemplateApplicationMode = TemplateApplicationModeView.ExtensionsOnly;
        page.ApplyTemplateDefaults(defaults);

        Assert.True(page.IsCertificateAuthority);
        Assert.True(page.HasPathLengthConstraint);
        Assert.Equal(2, page.PathLengthConstraint);
        Assert.Equal("DigitalSignature, KeyEncipherment", page.KeyUsages);
        Assert.Equal("Client Authentication", page.EnhancedKeyUsages);
        Assert.Equal(KeyAlgorithmKind.Rsa, page.KeyAlgorithm);

        page.SelectedTemplateApplicationMode = TemplateApplicationModeView.Full;
        page.ApplyTemplateDefaults(defaults);

        Assert.Equal("Template Display", page.DisplayName);
        Assert.Equal(KeyAlgorithmKind.Ecdsa, page.KeyAlgorithm);
        Assert.Equal(EllipticCurveKind.P384, page.Curve);
        Assert.Equal("SHA-384", page.SignatureAlgorithm);
        Assert.Equal(397, page.ValidityDays);
    }

    [Fact]
    public void TemplatesPageViewModel_ShouldLoadCertificateAndRequestIntoCentralAuthoringSurface()
    {
        var page = new TemplatesPageViewModel();
        var certificate = CreateCertificateListItem(Guid.NewGuid(), "Leaf");
        var request = new CertificateRequestListItem(
            Guid.NewGuid(),
            "Leaf CSR",
            "CN=leaf.example.test",
            Guid.NewGuid(),
            new NavigationTarget(BrowserEntityType.PrivateKey, Guid.NewGuid(), NavigationFocusSection.Overview),
            "ECDSA",
            "leaf.example.test, api.example.test",
            DateTimeOffset.UtcNow);
        var inspector = new CertificateInspectorData(
            certificate.CertificateId,
            new CertificateDisplayFields(certificate.DisplayName, "range", "Leaf", "Issuer", "Leaf Key"),
            new CertificateRawFields(
                certificate.Subject,
                certificate.Issuer,
                certificate.SerialNumber,
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(364),
                certificate.Sha1Thumbprint,
                certificate.Sha256Thumbprint,
                certificate.KeyAlgorithm),
            new CertificateExtensionFields(
                false,
                ["leaf.example.test"],
                ["DigitalSignature", "KeyEncipherment"],
                ["Server Authentication"]),
            new CertificateRevocationInfo(false, "Active", null, null, null),
            new CertificateNavigationInfo(null, null, []));

        page.PrepareTemplateFromCertificate(certificate, inspector);

        Assert.Equal("Leaf derived template", page.Name);
        Assert.Equal(TemplateIntendedUsage.EndEntityCertificate, page.IntendedUsage);
        Assert.Equal("CN=Leaf", page.Authoring.SubjectName);
        Assert.Equal("leaf.example.test", page.Authoring.SubjectAlternativeNames);
        Assert.Equal("DigitalSignature, KeyEncipherment", page.Authoring.KeyUsages);

        page.PrepareTemplateFromCertificateRequest(request);

        Assert.Equal("Leaf CSR derived template", page.Name);
        Assert.Equal(TemplateIntendedUsage.CertificateSigningRequest, page.IntendedUsage);
        Assert.Equal("CN=leaf.example.test", page.Authoring.SubjectName);
        Assert.Equal("leaf.example.test, api.example.test", page.Authoring.SubjectAlternativeNames);
        Assert.Equal(KeyAlgorithmKind.Ecdsa, page.Authoring.KeyAlgorithm);
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
