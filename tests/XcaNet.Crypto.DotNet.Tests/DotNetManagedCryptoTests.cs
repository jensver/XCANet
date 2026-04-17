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
}
