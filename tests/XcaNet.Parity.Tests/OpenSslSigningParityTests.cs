using XcaNet.Contracts.Crypto;
using XcaNet.Crypto.DotNet;
using XcaNet.Crypto.OpenSsl;
using XcaNet.Interop.OpenSsl;
using XcaNet.Tests.Shared;

namespace XcaNet.Parity.Tests;

public sealed class OpenSslSigningParityTests
{
    [Fact]
    public async Task SignCertificateSigningRequest_ShouldPreserveCoreFieldsAcrossManagedAndOpenSslPaths()
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

        var issuerKey = await managed.GenerateAsync(new GenerateKeyPairRequest("Issuer", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var issuerCert = await managed.CreateSelfSignedCaAsync(new SelfSignedCaCertificateRequest("CN=Issuer", issuerKey.Value!.Pkcs8PrivateKey, issuerKey.Value.Algorithm, 365), CancellationToken.None);
        var leafKey = await managed.GenerateAsync(new GenerateKeyPairRequest("Leaf", KeyAlgorithmKind.Ecdsa, null, EllipticCurveKind.P256), CancellationToken.None);
        var csr = await managed.CreateAsync(new CreateCertificateSigningRequestRequest("CN=leaf.example.test", leafKey.Value!.Pkcs8PrivateKey, leafKey.Value.Algorithm, [new SanEntry("leaf.example.test")]), CancellationToken.None);

        var managedResult = await managed.SignCertificateSigningRequestAsync(
            new SignCertificateSigningRequestRequest(csr.Value!.DerData, issuerCert.Value!.DerData, issuerKey.Value.Pkcs8PrivateKey, issuerKey.Value.Algorithm, 180, CryptoBackendPreference.PreferManaged),
            CancellationToken.None);
        var openSslResult = await openSslBackend.SignCertificateSigningRequestAsync(
            new SignCertificateSigningRequestRequest(csr.Value.DerData, issuerCert.Value.DerData, issuerKey.Value.Pkcs8PrivateKey, issuerKey.Value.Algorithm, 180, CryptoBackendPreference.PreferOpenSsl),
            CancellationToken.None);

        Assert.True(managedResult.IsSuccess, managedResult.Message);
        Assert.True(openSslResult.IsSuccess, openSslResult.Message);
        Assert.Equal(CryptoBackendKind.Managed, managedResult.Value!.BackendUsed);
        Assert.Equal(CryptoBackendKind.OpenSsl, openSslResult.Value!.BackendUsed);

        var parsedManaged = await managed.ParseCertificateAsync(new CertificateParseRequest(managedResult.Value.DerData, CryptoDataFormat.Der), CancellationToken.None);
        var parsedOpenSsl = await managed.ParseCertificateAsync(new CertificateParseRequest(openSslResult.Value.DerData, CryptoDataFormat.Der), CancellationToken.None);

        Assert.True(parsedManaged.IsSuccess);
        Assert.True(parsedOpenSsl.IsSuccess);
        Assert.Equal(parsedManaged.Value!.Subject, parsedOpenSsl.Value!.Subject);
        Assert.Equal(parsedManaged.Value.Issuer, parsedOpenSsl.Value.Issuer);
        Assert.Equal(parsedManaged.Value.SubjectAlternativeNames, parsedOpenSsl.Value.SubjectAlternativeNames);
        Assert.Equal(parsedManaged.Value.IsCertificateAuthority, parsedOpenSsl.Value.IsCertificateAuthority);
    }
}
