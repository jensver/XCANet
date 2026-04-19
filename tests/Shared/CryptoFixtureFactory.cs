using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using XcaNet.Contracts.Crypto;

namespace XcaNet.Tests.Shared;

internal sealed record ExtensionRichCertificateSigningRequestFixture(
    byte[] DerData,
    IReadOnlyList<string> SubjectAlternativeNames,
    IReadOnlyList<string> KeyUsages,
    IReadOnlyList<string> EnhancedKeyUsages);

internal static class CryptoFixtureFactory
{
    public static ExtensionRichCertificateSigningRequestFixture CreateExtensionRichCertificateSigningRequest(
        byte[] privateKeyPkcs8,
        string privateKeyAlgorithm,
        string subjectName,
        IReadOnlyList<string> subjectAlternativeNames)
    {
        using var key = LoadPrivateKey(privateKeyPkcs8, privateKeyAlgorithm);
        var distinguishedName = new X500DistinguishedName(subjectName);
        var request = key.Algorithm switch
        {
            "RSA" => new CertificateRequest(distinguishedName, key.Rsa!, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
            "ECDSA" => new CertificateRequest(distinguishedName, key.Ecdsa!, HashAlgorithmName.SHA256),
            _ => throw new InvalidOperationException($"Unsupported key algorithm '{privateKeyAlgorithm}'.")
        };

        var sanBuilder = new SubjectAlternativeNameBuilder();
        foreach (var san in subjectAlternativeNames)
        {
            sanBuilder.AddDnsName(san);
        }

        request.CertificateExtensions.Add(sanBuilder.Build());

        const X509KeyUsageFlags keyUsageFlags =
            X509KeyUsageFlags.DigitalSignature
            | X509KeyUsageFlags.KeyEncipherment
            | X509KeyUsageFlags.DataEncipherment;

        request.CertificateExtensions.Add(new X509KeyUsageExtension(keyUsageFlags, true));

        var ekuCollection = new OidCollection
        {
            new("1.3.6.1.5.5.7.3.1", "Server Authentication"),
            new("1.3.6.1.5.5.7.3.2", "Client Authentication"),
            new("1.3.6.1.5.5.7.3.4", "Secure Email")
        };
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(ekuCollection, false));

        return new ExtensionRichCertificateSigningRequestFixture(
            request.CreateSigningRequest(),
            subjectAlternativeNames.ToArray(),
            ["DigitalSignature", "KeyEncipherment", "DataEncipherment"],
            ["Server Authentication", "Client Authentication", "Secure Email"]);
    }

    private static LoadedPrivateKey LoadPrivateKey(byte[] privateKeyPkcs8, string algorithm)
    {
        return algorithm switch
        {
            "RSA" => LoadedPrivateKey.FromRsa(ImportRsa(privateKeyPkcs8)),
            "ECDSA" => LoadedPrivateKey.FromEcdsa(ImportEcdsa(privateKeyPkcs8)),
            _ => throw new InvalidOperationException($"Unsupported key algorithm '{algorithm}'.")
        };
    }

    private static RSA ImportRsa(byte[] privateKeyPkcs8)
    {
        var rsa = RSA.Create();
        rsa.ImportPkcs8PrivateKey(privateKeyPkcs8, out _);
        return rsa;
    }

    private static ECDsa ImportEcdsa(byte[] privateKeyPkcs8)
    {
        var ecdsa = ECDsa.Create();
        ecdsa.ImportPkcs8PrivateKey(privateKeyPkcs8, out _);
        return ecdsa;
    }

    private sealed class LoadedPrivateKey : IDisposable
    {
        private LoadedPrivateKey(RSA? rsa, ECDsa? ecdsa, string algorithm)
        {
            Rsa = rsa;
            Ecdsa = ecdsa;
            Algorithm = algorithm;
        }

        public RSA? Rsa { get; }

        public ECDsa? Ecdsa { get; }

        public string Algorithm { get; }

        public static LoadedPrivateKey FromRsa(RSA rsa) => new(rsa, null, "RSA");

        public static LoadedPrivateKey FromEcdsa(ECDsa ecdsa) => new(null, ecdsa, "ECDSA");

        public void Dispose()
        {
            Rsa?.Dispose();
            Ecdsa?.Dispose();
        }
    }
}
