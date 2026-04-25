using XcaNet.Contracts.Crypto;
using XcaNet.Crypto.DotNet;
using XcaNet.Crypto.OpenSsl;
using XcaNet.Interop.OpenSsl;
using XcaNet.Tests.Shared;
using XcaNet.Contracts.Revocation;

namespace XcaNet.Parity.Tests;

public sealed class OpenSslSigningParityTests
{
    [Fact]
    public async Task SignCertificateSigningRequest_ShouldPreserveCoreFieldsAcrossManagedAndOpenSslPaths()
    {
        var build = OpenSslBridgeTestHarness.BuildNativeBridge();
        if (!build.IsSuccess) return;

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

    [Fact]
    public async Task SignCertificateSigningRequest_WithSanHeavyExtensionRichCsr_ShouldPreserveNormalizedExtensions()
    {
        var build = OpenSslBridgeTestHarness.BuildNativeBridge();
        if (!build.IsSuccess) return;

        var managed = new DotNetCryptoBackend();
        var openSslBackend = new OpenSslCryptoBackend(
            managed,
            new OpenSslBridgeClient(new OpenSslBridgeOptions
            {
                LibraryPath = build.LibraryPath
            }));

        var issuerKey = await managed.GenerateAsync(new GenerateKeyPairRequest("Issuer", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var issuerCert = await managed.CreateSelfSignedCaAsync(new SelfSignedCaCertificateRequest("CN=Issuer", issuerKey.Value!.Pkcs8PrivateKey, issuerKey.Value.Algorithm, 365), CancellationToken.None);
        var leafKey = await managed.GenerateAsync(new GenerateKeyPairRequest("Leaf", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var fixture = CryptoFixtureFactory.CreateExtensionRichCertificateSigningRequest(
            leafKey.Value!.Pkcs8PrivateKey,
            leafKey.Value.Algorithm,
            "CN=edge.example.test",
            ["edge.example.test", "alt1.example.test", "alt2.example.test", "alt3.example.test"]);

        var managedResult = await managed.SignCertificateSigningRequestAsync(
            new SignCertificateSigningRequestRequest(fixture.DerData, issuerCert.Value!.DerData, issuerKey.Value.Pkcs8PrivateKey, issuerKey.Value.Algorithm, 180, CryptoBackendPreference.PreferManaged),
            CancellationToken.None);
        var openSslResult = await openSslBackend.SignCertificateSigningRequestAsync(
            new SignCertificateSigningRequestRequest(fixture.DerData, issuerCert.Value.DerData, issuerKey.Value.Pkcs8PrivateKey, issuerKey.Value.Algorithm, 180, CryptoBackendPreference.PreferOpenSsl),
            CancellationToken.None);

        Assert.True(managedResult.IsSuccess, managedResult.Message);
        Assert.True(openSslResult.IsSuccess, openSslResult.Message);

        var parsedManaged = await managed.ParseCertificateAsync(new CertificateParseRequest(managedResult.Value!.DerData, CryptoDataFormat.Der), CancellationToken.None);
        var parsedOpenSsl = await managed.ParseCertificateAsync(new CertificateParseRequest(openSslResult.Value!.DerData, CryptoDataFormat.Der), CancellationToken.None);

        Assert.True(parsedManaged.IsSuccess);
        Assert.True(parsedOpenSsl.IsSuccess);
        Assert.Equal(fixture.SubjectAlternativeNames.OrderBy(x => x), parsedManaged.Value!.SubjectAlternativeNames.OrderBy(x => x));
        Assert.Equal(parsedManaged.Value.SubjectAlternativeNames.OrderBy(x => x), parsedOpenSsl.Value!.SubjectAlternativeNames.OrderBy(x => x));
        Assert.Equal(parsedManaged.Value.KeyUsages.OrderBy(x => x), parsedOpenSsl.Value.KeyUsages.OrderBy(x => x));
        Assert.Equal(parsedManaged.Value.EnhancedKeyUsages.OrderBy(x => x), parsedOpenSsl.Value.EnhancedKeyUsages.OrderBy(x => x));
        Assert.False(parsedManaged.Value.IsCertificateAuthority);
        Assert.False(parsedOpenSsl.Value.IsCertificateAuthority);
    }

    [Fact]
    public async Task ManagedPkcs12Export_ShouldBeReadableByOpenSslCli()
    {
        if (!OpenSslCliHarness.IsAvailable())
        {
            return;
        }

        var managed = new DotNetCryptoBackend();
        var key = await managed.GenerateAsync(new GenerateKeyPairRequest("Bundle Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var certificate = await managed.CreateSelfSignedCaAsync(new SelfSignedCaCertificateRequest("CN=Bundle CA", key.Value!.Pkcs8PrivateKey, key.Value.Algorithm, 365), CancellationToken.None);
        var export = await managed.ExportPkcs12Async(
            new ExportPkcs12Request(certificate.Value!.DerData, key.Value.Pkcs8PrivateKey, key.Value.Algorithm, "bundle", "bundle-password"),
            CancellationToken.None);

        Assert.True(export.IsSuccess, export.Message);

        var filePath = Path.Combine(Path.GetTempPath(), $"xcanet-parity-{Guid.NewGuid():N}.pfx");
        await File.WriteAllBytesAsync(filePath, export.Value!.Data);

        try
        {
            var result = OpenSslCliHarness.Run("pkcs12", "-in", filePath, "-passin", "pass:bundle-password", "-nokeys", "-clcerts");
            Assert.Equal(0, result.ExitCode);
            Assert.Contains("BEGIN CERTIFICATE", result.StandardOutput);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ManagedGeneratedCrl_ShouldBeReadableByOpenSslCliAndContainRevokedSerials()
    {
        if (!OpenSslCliHarness.IsAvailable())
        {
            return;
        }

        var managed = new DotNetCryptoBackend();
        var issuerKey = await managed.GenerateAsync(new GenerateKeyPairRequest("Issuer", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var issuerCertificate = await managed.CreateSelfSignedCaAsync(new SelfSignedCaCertificateRequest("CN=Issuer CA", issuerKey.Value!.Pkcs8PrivateKey, issuerKey.Value.Algorithm, 365), CancellationToken.None);
        var leafKey = await managed.GenerateAsync(new GenerateKeyPairRequest("Leaf", KeyAlgorithmKind.Ecdsa, null, EllipticCurveKind.P256), CancellationToken.None);
        var csr = await managed.CreateAsync(
            new CreateCertificateSigningRequestRequest("CN=revoked.example.test", leafKey.Value!.Pkcs8PrivateKey, leafKey.Value.Algorithm, [new SanEntry("revoked.example.test")]),
            CancellationToken.None);
        var leafCertificate = await managed.SignCertificateSigningRequestAsync(
            new SignCertificateSigningRequestRequest(csr.Value!.DerData, issuerCertificate.Value!.DerData, issuerKey.Value.Pkcs8PrivateKey, issuerKey.Value.Algorithm, 180),
            CancellationToken.None);

        var crl = await managed.GenerateCertificateRevocationListAsync(
            new GenerateCertificateRevocationListRequest(
                issuerCertificate.Value.DerData,
                issuerKey.Value.Pkcs8PrivateKey,
                issuerKey.Value.Algorithm,
                42,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddDays(7),
                [new RevokedCertificateEntry(Guid.Empty, "Leaf", "CN=revoked.example.test", leafCertificate.Value!.Details.SerialNumber, CertificateRevocationReason.KeyCompromise, DateTimeOffset.UtcNow)]),
            CancellationToken.None);

        Assert.True(crl.IsSuccess, crl.Message);

        var filePath = Path.Combine(Path.GetTempPath(), $"xcanet-parity-{Guid.NewGuid():N}.crl.pem");
        await File.WriteAllTextAsync(filePath, crl.Value!.PemData);

        try
        {
            var result = OpenSslCliHarness.Run("crl", "-in", filePath, "-inform", "PEM", "-text", "-noout");
            var expectedSerial = NormalizeSerialHex(leafCertificate.Value.Details.SerialNumber);
            var normalizedOutput = NormalizeHex(result.StandardOutput);
            Assert.Equal(0, result.ExitCode);
            Assert.Contains("Issuer CA", result.StandardOutput);
            Assert.Contains(expectedSerial, normalizedOutput);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task MalformedInputs_ShouldFailDeterministicallyAcrossBackends()
    {
        var build = OpenSslBridgeTestHarness.BuildNativeBridge();
        if (!build.IsSuccess) return;

        var managed = new DotNetCryptoBackend();
        var openSslBackend = new OpenSslCryptoBackend(
            managed,
            new OpenSslBridgeClient(new OpenSslBridgeOptions
            {
                LibraryPath = build.LibraryPath
            }));

        var issuerKey = await managed.GenerateAsync(new GenerateKeyPairRequest("Issuer", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var issuerCert = await managed.CreateSelfSignedCaAsync(new SelfSignedCaCertificateRequest("CN=Issuer", issuerKey.Value!.Pkcs8PrivateKey, issuerKey.Value.Algorithm, 365), CancellationToken.None);
        var malformedRequest = new SignCertificateSigningRequestRequest([1, 2, 3, 4], issuerCert.Value!.DerData, issuerKey.Value.Pkcs8PrivateKey, issuerKey.Value.Algorithm, 180);

        var managedResult = await managed.SignCertificateSigningRequestAsync(malformedRequest, CancellationToken.None);
        var openSslResult = await openSslBackend.SignCertificateSigningRequestAsync(malformedRequest with { PreferredBackend = CryptoBackendPreference.PreferOpenSsl }, CancellationToken.None);

        Assert.False(managedResult.IsSuccess);
        Assert.False(openSslResult.IsSuccess);
    }

    private static string NormalizeWhitespace(string value)
    {
        return string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string NormalizeHex(string value)
    {
        return new string(value.Where(Uri.IsHexDigit).ToArray()).ToUpperInvariant();
    }

    private static string NormalizeSerialHex(string value)
    {
        var normalized = NormalizeHex(value).TrimStart('0');
        return string.IsNullOrEmpty(normalized) ? "0" : normalized;
    }
}
