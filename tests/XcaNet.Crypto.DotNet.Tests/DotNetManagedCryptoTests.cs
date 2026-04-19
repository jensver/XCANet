using XcaNet.Contracts.Crypto;
using XcaNet.Crypto.DotNet;

namespace XcaNet.Crypto.DotNet.Tests;

public sealed class DotNetManagedCryptoTests
{
    private readonly DotNetCryptoBackend _backend = new();

    [Fact]
    public async Task GenerateAsync_WithWeakRsaKeySize_ShouldFail()
    {
        var result = await _backend.GenerateAsync(
            new GenerateKeyPairRequest("Weak RSA", KeyAlgorithmKind.Rsa, 2048, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CreateSelfSignedCaAsync_ShouldProduceParseableCertificate()
    {
        var keyResult = await _backend.GenerateAsync(
            new GenerateKeyPairRequest("Root Key", KeyAlgorithmKind.Rsa, 3072, null),
            CancellationToken.None);

        var certificateResult = await _backend.CreateSelfSignedCaAsync(
            new SelfSignedCaCertificateRequest("CN=Root CA", keyResult.Value!.Pkcs8PrivateKey, keyResult.Value.Algorithm, 365),
            CancellationToken.None);

        Assert.True(certificateResult.IsSuccess);
        Assert.True(certificateResult.Value!.Details.IsCertificateAuthority);
        Assert.Equal("CN=Root CA", certificateResult.Value.Details.Subject);
    }

    [Fact]
    public async Task CreateAndParseCertificateSigningRequest_ShouldRoundTripSubjectAndSans()
    {
        var keyResult = await _backend.GenerateAsync(
            new GenerateKeyPairRequest("Leaf Key", KeyAlgorithmKind.Ecdsa, null, EllipticCurveKind.P256),
            CancellationToken.None);

        var createResult = await _backend.CreateAsync(
            new CreateCertificateSigningRequestRequest(
                "CN=service.example.test",
                keyResult.Value!.Pkcs8PrivateKey,
                keyResult.Value.Algorithm,
                [new SanEntry("service.example.test")]),
            CancellationToken.None);

        var parseResult = await _backend.ParseAsync(
            new CertificateSigningRequestParseRequest(createResult.Value!.DerData, CryptoDataFormat.Pkcs10),
            CancellationToken.None);

        Assert.True(parseResult.IsSuccess);
        Assert.Equal("CN=service.example.test", parseResult.Value!.Subject);
        Assert.Contains("service.example.test", parseResult.Value.SubjectAlternativeNames);
    }

    [Fact]
    public async Task ImportAsync_WithMalformedPkcs12Bundle_ShouldFail()
    {
        var result = await _backend.ImportAsync(
            new ImportCertificateMaterialRequest(CryptoImportKind.Bundle, CryptoDataFormat.Pkcs12, [1, 2, 3, 4], "wrong", "Broken Bundle"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ImportAsync_WithWrongPkcs12Password_ShouldFail()
    {
        var keyResult = await _backend.GenerateAsync(
            new GenerateKeyPairRequest("Bundle Key", KeyAlgorithmKind.Rsa, 3072, null),
            CancellationToken.None);
        var certificateResult = await _backend.CreateSelfSignedCaAsync(
            new SelfSignedCaCertificateRequest("CN=Bundle CA", keyResult.Value!.Pkcs8PrivateKey, keyResult.Value.Algorithm, 365),
            CancellationToken.None);
        var exportResult = await _backend.ExportPkcs12Async(
            new ExportPkcs12Request(certificateResult.Value!.DerData, keyResult.Value.Pkcs8PrivateKey, keyResult.Value.Algorithm, "bundle", "correct-password"),
            CancellationToken.None);

        var importResult = await _backend.ImportAsync(
            new ImportCertificateMaterialRequest(CryptoImportKind.Bundle, CryptoDataFormat.Pkcs12, exportResult.Value!.Data, "wrong-password", "Broken Bundle"),
            CancellationToken.None);

        Assert.False(importResult.IsSuccess);
    }
}
