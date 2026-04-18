using XcaNet.Contracts.Crypto;
using XcaNet.Crypto.DotNet;
using XcaNet.Interop.OpenSsl;
using XcaNet.Tests.Shared;

namespace XcaNet.Crypto.OpenSsl.Tests;

public sealed class OpenSslCryptoBackendTests
{
    [Fact]
    public void Diagnostics_ShouldReportUnavailableWhenBridgeMissing()
    {
        var backend = new OpenSslCryptoBackend(
            new DotNetCryptoBackend(),
            new OpenSslBridgeClient(new OpenSslBridgeOptions
            {
                LibraryPath = Path.Combine(Path.GetTempPath(), "missing-xcanet-bridge.dylib")
            }));

        Assert.Equal("OpenSSL", backend.Name);
        Assert.False(backend.Diagnostics.IsAvailable);
    }

    [Fact]
    public async Task SignCertificateSigningRequestAsync_WhenBridgeAvailable_ShouldUseOpenSsl()
    {
        var build = OpenSslBridgeTestHarness.BuildNativeBridge();
        Assert.True(build.IsSuccess, build.FailureReason);

        var managed = new DotNetCryptoBackend();
        var backend = new OpenSslCryptoBackend(
            managed,
            new OpenSslBridgeClient(new OpenSslBridgeOptions
            {
                LibraryPath = build.LibraryPath
            }));

        var issuerKey = await managed.GenerateAsync(new GenerateKeyPairRequest("Issuer", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var issuerCert = await managed.CreateSelfSignedCaAsync(new SelfSignedCaCertificateRequest("CN=Issuer", issuerKey.Value!.Pkcs8PrivateKey, issuerKey.Value.Algorithm, 365), CancellationToken.None);
        var leafKey = await managed.GenerateAsync(new GenerateKeyPairRequest("Leaf", KeyAlgorithmKind.Ecdsa, null, EllipticCurveKind.P256), CancellationToken.None);
        var csr = await managed.CreateAsync(new CreateCertificateSigningRequestRequest("CN=leaf.example.test", leafKey.Value!.Pkcs8PrivateKey, leafKey.Value.Algorithm, [new SanEntry("leaf.example.test")]), CancellationToken.None);

        var result = await backend.SignCertificateSigningRequestAsync(
            new SignCertificateSigningRequestRequest(csr.Value!.DerData, issuerCert.Value!.DerData, issuerKey.Value.Pkcs8PrivateKey, issuerKey.Value.Algorithm, 180, CryptoBackendPreference.PreferOpenSsl),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Message);
        Assert.Equal(CryptoBackendKind.OpenSsl, result.Value!.BackendUsed);
    }

    [Fact]
    public void RoutingPolicy_ShouldSelectManagedByDefault()
    {
        var policy = new CryptoBackendRoutingPolicy(new CryptoBackendRoutingOptions(), new OpenSslDiagnosticsSnapshot(false, null, OpenSslBridgeCapabilities.None, "missing", null));

        var decision = policy.SelectCertificateSigningBackend(null);

        Assert.Equal(CryptoBackendKind.Managed, decision.BackendToUse);
    }

    [Fact]
    public void RoutingPolicy_ShouldFallBackToManagedWhenOpenSslUnavailable()
    {
        var policy = new CryptoBackendRoutingPolicy(
            new CryptoBackendRoutingOptions
            {
                DefaultPreference = CryptoBackendPreference.PreferOpenSsl
            },
            new OpenSslDiagnosticsSnapshot(false, null, OpenSslBridgeCapabilities.None, "missing", null));

        var decision = policy.SelectCertificateSigningBackend(CryptoBackendPreference.PreferOpenSsl);

        Assert.Equal(CryptoBackendKind.Managed, decision.BackendToUse);
        Assert.True(decision.FellBackToManaged);
    }

    [Fact]
    public void RoutingPolicy_ShouldPreferManagedEvenWhenOpenSslIsAvailable()
    {
        var policy = new CryptoBackendRoutingPolicy(
            new CryptoBackendRoutingOptions(),
            new OpenSslDiagnosticsSnapshot(true, "OpenSSL 3", OpenSslBridgeCapabilities.SupportsCertificateSigningRequestSigning, null, "/tmp/libxcanet_ossl_bridge.dylib"));

        var decision = policy.SelectCertificateSigningBackend(CryptoBackendPreference.PreferManaged);

        Assert.Equal(CryptoBackendKind.Managed, decision.BackendToUse);
        Assert.False(decision.FellBackToManaged);
    }

    [Fact]
    public void RoutingPolicy_OpenSslOnlyWithoutCapability_ShouldNotPretendManagedFallback()
    {
        var policy = new CryptoBackendRoutingPolicy(
            new CryptoBackendRoutingOptions(),
            new OpenSslDiagnosticsSnapshot(false, null, OpenSslBridgeCapabilities.None, "bridge missing", null));

        var decision = policy.SelectCertificateSigningBackend(CryptoBackendPreference.OpenSslOnly);

        Assert.Equal(CryptoBackendKind.OpenSsl, decision.BackendToUse);
        Assert.False(decision.FellBackToManaged);
    }

    [Fact]
    public async Task RoutedService_WhenOpenSslOnlyAndBridgeMissing_ShouldFail()
    {
        var managed = new DotNetCryptoBackend();
        var openSslBackend = new OpenSslCryptoBackend(
            managed,
            new OpenSslBridgeClient(new OpenSslBridgeOptions
            {
                LibraryPath = Path.Combine(Path.GetTempPath(), "missing-xcanet-bridge.dylib")
            }));
        var routed = new RoutedCertificateService(
            managed,
            openSslBackend,
            new CryptoBackendRoutingOptions
            {
                DefaultPreference = CryptoBackendPreference.PreferManaged
            });

        var issuerKey = await managed.GenerateAsync(new GenerateKeyPairRequest("Issuer", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var issuerCert = await managed.CreateSelfSignedCaAsync(new SelfSignedCaCertificateRequest("CN=Issuer", issuerKey.Value!.Pkcs8PrivateKey, issuerKey.Value.Algorithm, 365), CancellationToken.None);
        var leafKey = await managed.GenerateAsync(new GenerateKeyPairRequest("Leaf", KeyAlgorithmKind.Ecdsa, null, EllipticCurveKind.P256), CancellationToken.None);
        var csr = await managed.CreateAsync(new CreateCertificateSigningRequestRequest("CN=leaf.example.test", leafKey.Value!.Pkcs8PrivateKey, leafKey.Value.Algorithm, [new SanEntry("leaf.example.test")]), CancellationToken.None);

        var result = await routed.SignCertificateSigningRequestAsync(
            new SignCertificateSigningRequestRequest(csr.Value!.DerData, issuerCert.Value!.DerData, issuerKey.Value.Pkcs8PrivateKey, issuerKey.Value.Algorithm, 180, CryptoBackendPreference.OpenSslOnly),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task RoutedService_WhenManagedPreferredAndBridgeAvailable_ShouldStayManaged()
    {
        var build = OpenSslBridgeTestHarness.BuildNativeBridge();
        Assert.True(build.IsSuccess, build.FailureReason);

        var managed = new DotNetCryptoBackend();
        var openSslBackend = new OpenSslCryptoBackend(
            managed,
            new OpenSslBridgeClient(new OpenSslBridgeOptions
            {
                LibraryPath = build.LibraryPath
            }));
        var routed = new RoutedCertificateService(managed, openSslBackend, new CryptoBackendRoutingOptions());

        var issuerKey = await managed.GenerateAsync(new GenerateKeyPairRequest("Issuer", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var issuerCert = await managed.CreateSelfSignedCaAsync(new SelfSignedCaCertificateRequest("CN=Issuer", issuerKey.Value!.Pkcs8PrivateKey, issuerKey.Value.Algorithm, 365), CancellationToken.None);
        var leafKey = await managed.GenerateAsync(new GenerateKeyPairRequest("Leaf", KeyAlgorithmKind.Ecdsa, null, EllipticCurveKind.P256), CancellationToken.None);
        var csr = await managed.CreateAsync(new CreateCertificateSigningRequestRequest("CN=leaf.example.test", leafKey.Value!.Pkcs8PrivateKey, leafKey.Value.Algorithm, [new SanEntry("leaf.example.test")]), CancellationToken.None);

        var result = await routed.SignCertificateSigningRequestAsync(
            new SignCertificateSigningRequestRequest(csr.Value!.DerData, issuerCert.Value!.DerData, issuerKey.Value.Pkcs8PrivateKey, issuerKey.Value.Algorithm, 180, CryptoBackendPreference.PreferManaged),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Message);
        Assert.Equal(CryptoBackendKind.Managed, result.Value!.BackendUsed);
    }
}
