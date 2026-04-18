using System.Formats.Asn1;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using XcaNet.Contracts.Revocation;
using XcaNet.Contracts.Crypto;
using XcaNet.Contracts.Results;
using XcaNet.Crypto.Abstractions;

namespace XcaNet.Crypto.DotNet;

public sealed class DotNetCryptoBackend : IKeyService, ICertificateService, ICertificateSigningRequestService, IImportExportService
{
    private const int MinimumRsaKeySize = 3072;

    public string Name => "Managed .NET";

    public Task<OperationResult<GenerateKeyPairResult>> GenerateAsync(GenerateKeyPairRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return Task.FromResult(OperationResult<GenerateKeyPairResult>.Failure(OperationErrorCode.ValidationFailed, "A display name is required."));
        }

        return request.Algorithm switch
        {
            KeyAlgorithmKind.Rsa => Task.FromResult(GenerateRsaKey(request)),
            KeyAlgorithmKind.Ecdsa => Task.FromResult(GenerateEcdsaKey(request)),
            _ => Task.FromResult(OperationResult<GenerateKeyPairResult>.Failure(OperationErrorCode.ValidationFailed, "Unsupported key algorithm."))
        };
    }

    public Task<OperationResult<PrivateKeyImportResult>> ImportPrivateKeyAsync(PrivateKeyImportRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var loadedKey = LoadPrivateKey(request.Data, request.Format);
            var pkcs8 = loadedKey.ExportPkcs8PrivateKey();
            var spki = loadedKey.ExportSubjectPublicKeyInfo();
            return Task.FromResult(OperationResult<PrivateKeyImportResult>.Success(
                new PrivateKeyImportResult(
                    request.DisplayName,
                    loadedKey.Algorithm,
                    ComputeFingerprint(spki),
                    pkcs8,
                    spki),
                "Private key imported."));
        }
        catch (CryptographicException)
        {
            return Task.FromResult(OperationResult<PrivateKeyImportResult>.Failure(OperationErrorCode.ValidationFailed, "Unsupported or invalid private key material."));
        }
    }

    public Task<OperationResult<ExportedArtifact>> ExportPrivateKeyAsync(PrivateKeyExportRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var loadedKey = LoadPrivateKey(request.Pkcs8PrivateKey, CryptoDataFormat.Pkcs8);
            var artifact = request.Format switch
            {
                CryptoDataFormat.Pem => ExportPrivateKeyPem(request, loadedKey),
                CryptoDataFormat.Der or CryptoDataFormat.Pkcs8 => ExportPrivateKeyBinary(request, loadedKey),
                _ => null
            };

            return artifact is null
                ? Task.FromResult(OperationResult<ExportedArtifact>.Failure(OperationErrorCode.ValidationFailed, "Unsupported private key export format."))
                : Task.FromResult(OperationResult<ExportedArtifact>.Success(artifact, "Private key exported."));
        }
        catch (CryptographicException)
        {
            return Task.FromResult(OperationResult<ExportedArtifact>.Failure(OperationErrorCode.ValidationFailed, "Failed to export the private key."));
        }
    }

    public Task<OperationResult<SignedCertificateResult>> CreateSelfSignedCaAsync(SelfSignedCaCertificateRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var loadedKey = LoadPrivateKey(request.Pkcs8PrivateKey, CryptoDataFormat.Pkcs8);
            var certificateRequest = CreateCertificateRequest(request.SubjectName, loadedKey, []);
            certificateRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            certificateRequest.CertificateExtensions.Add(new X509KeyUsageExtension(
                X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign | X509KeyUsageFlags.DigitalSignature,
                true));
            certificateRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(certificateRequest.PublicKey, false));

            var notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
            var notAfter = notBefore.AddDays(Math.Max(1, request.ValidityDays));
            using var certificate = certificateRequest.CreateSelfSigned(notBefore, notAfter);
            var der = certificate.Export(X509ContentType.Cert);
            return Task.FromResult(OperationResult<SignedCertificateResult>.Success(
                new SignedCertificateResult(der, ParseCertificate(certificate), CryptoBackendKind.Managed),
                "Self-signed CA certificate created."));
        }
        catch (CryptographicException)
        {
            return Task.FromResult(OperationResult<SignedCertificateResult>.Failure(OperationErrorCode.ValidationFailed, "Failed to create a self-signed certificate."));
        }
    }

    public Task<OperationResult<SignedCertificateResult>> SignCertificateSigningRequestAsync(SignCertificateSigningRequestRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var issuerCertificate = X509CertificateLoader.LoadCertificate(request.IssuerCertificateDer);
            using var issuerKey = LoadPrivateKey(request.IssuerPrivateKeyPkcs8, CryptoDataFormat.Pkcs8);
            var loadedRequest = CertificateRequest.LoadSigningRequest(
                request.CertificateSigningRequestDer,
                HashAlgorithmName.SHA256,
                CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions,
                RSASignaturePadding.Pkcs1);

            using var issuedCertificate = loadedRequest.Create(
                issuerCertificate.SubjectName,
                CreateSignatureGenerator(issuerKey),
                DateTimeOffset.UtcNow.AddMinutes(-5),
                DateTimeOffset.UtcNow.AddDays(Math.Max(1, request.ValidityDays)),
                RandomNumberGenerator.GetBytes(16));

            var der = issuedCertificate.Export(X509ContentType.Cert);
            return Task.FromResult(OperationResult<SignedCertificateResult>.Success(
                new SignedCertificateResult(der, ParseCertificate(issuedCertificate), CryptoBackendKind.Managed),
                "Certificate signing request issued."));
        }
        catch (CryptographicException)
        {
            return Task.FromResult(OperationResult<SignedCertificateResult>.Failure(OperationErrorCode.ValidationFailed, "Failed to sign the certificate signing request."));
        }
    }

    public Task<OperationResult<CertificateDetails>> ParseCertificateAsync(CertificateParseRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var certificate = LoadCertificate(request.Data, request.Format);
            return Task.FromResult(OperationResult<CertificateDetails>.Success(ParseCertificate(certificate), "Certificate parsed."));
        }
        catch (CryptographicException)
        {
            return Task.FromResult(OperationResult<CertificateDetails>.Failure(OperationErrorCode.ValidationFailed, "Invalid certificate data."));
        }
    }

    public Task<OperationResult<CertificateRevocationListResult>> GenerateCertificateRevocationListAsync(GenerateCertificateRevocationListRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var issuerCertificate = X509CertificateLoader.LoadCertificate(request.IssuerCertificateDer);
            using var issuerKey = LoadPrivateKey(request.IssuerPrivateKeyPkcs8, CryptoDataFormat.Pkcs8);
            using var issuerCertificateWithKey = AttachPrivateKey(issuerCertificate, issuerKey);
            var builder = new CertificateRevocationListBuilder();

            foreach (var revokedCertificate in request.RevokedCertificates)
            {
                builder.AddEntry(
                    ParseSerialNumber(revokedCertificate.SerialNumber),
                    revokedCertificate.RevokedAt,
                    MapRevocationReason(revokedCertificate.Reason));
            }

            var crlBytes = BuildCertificateRevocationList(
                builder,
                issuerCertificateWithKey,
                request.CrlNumber,
                request.ThisUpdate,
                request.NextUpdate,
                issuerKey.Algorithm);

            var pem = PemEncoding.WriteString("X509 CRL", crlBytes);
            var details = ParseCertificateRevocationList(crlBytes, CryptoDataFormat.Der);
            return Task.FromResult(OperationResult<CertificateRevocationListResult>.Success(
                new CertificateRevocationListResult(crlBytes, pem, details),
                "Certificate revocation list generated."));
        }
        catch (CryptographicException)
        {
            return Task.FromResult(OperationResult<CertificateRevocationListResult>.Failure(OperationErrorCode.ValidationFailed, "Failed to generate the certificate revocation list."));
        }
    }

    public Task<OperationResult<CertificateRevocationListDetails>> ParseCertificateRevocationListAsync(CertificateRevocationListParseRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return Task.FromResult(OperationResult<CertificateRevocationListDetails>.Success(
                ParseCertificateRevocationList(request.Data, request.Format),
                "Certificate revocation list parsed."));
        }
        catch (CryptographicException)
        {
            return Task.FromResult(OperationResult<CertificateRevocationListDetails>.Failure(OperationErrorCode.ValidationFailed, "Invalid certificate revocation list data."));
        }
    }

    public Task<OperationResult<CertificateSigningRequestResult>> CreateAsync(CreateCertificateSigningRequestRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var loadedKey = LoadPrivateKey(request.Pkcs8PrivateKey, CryptoDataFormat.Pkcs8);
            var certificateRequest = CreateCertificateRequest(request.SubjectName, loadedKey, request.SubjectAlternativeNames);
            var der = certificateRequest.CreateSigningRequest();
            return Task.FromResult(OperationResult<CertificateSigningRequestResult>.Success(
                new CertificateSigningRequestResult(
                    der,
                    new CertificateSigningRequestDetails(
                        certificateRequest.SubjectName.Name ?? request.SubjectName,
                        loadedKey.Algorithm,
                        request.SubjectAlternativeNames.Select(x => x.Value).ToArray())),
                "Certificate signing request created."));
        }
        catch (CryptographicException)
        {
            return Task.FromResult(OperationResult<CertificateSigningRequestResult>.Failure(OperationErrorCode.ValidationFailed, "Failed to create the certificate signing request."));
        }
    }

    public Task<OperationResult<CertificateSigningRequestDetails>> ParseAsync(CertificateSigningRequestParseRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var loadedRequest = request.Format == CryptoDataFormat.Pem
                ? CertificateRequest.LoadSigningRequestPem(
                    System.Text.Encoding.UTF8.GetString(request.Data),
                    HashAlgorithmName.SHA256,
                    CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions,
                    RSASignaturePadding.Pkcs1)
                : CertificateRequest.LoadSigningRequest(
                    request.Data,
                    HashAlgorithmName.SHA256,
                    CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions,
                    RSASignaturePadding.Pkcs1);

            return Task.FromResult(OperationResult<CertificateSigningRequestDetails>.Success(ParseCertificateSigningRequest(loadedRequest), "Certificate signing request parsed."));
        }
        catch (CryptographicException)
        {
            return Task.FromResult(OperationResult<CertificateSigningRequestDetails>.Failure(OperationErrorCode.ValidationFailed, "Invalid certificate signing request data."));
        }
    }

    public Task<OperationResult<ImportCertificateMaterialResult>> ImportAsync(ImportCertificateMaterialRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return request.Kind switch
            {
                CryptoImportKind.PrivateKey => ImportPrivateKeyMaterial(request),
                CryptoImportKind.Certificate => ImportCertificateMaterial(request),
                CryptoImportKind.CertificateSigningRequest => ImportCertificateSigningRequestMaterial(request),
                CryptoImportKind.Bundle => ImportBundleMaterial(request),
                _ => Task.FromResult(OperationResult<ImportCertificateMaterialResult>.Failure(OperationErrorCode.ValidationFailed, "Unsupported import kind."))
            };
        }
        catch (CryptographicException)
        {
            return Task.FromResult(OperationResult<ImportCertificateMaterialResult>.Failure(OperationErrorCode.ValidationFailed, "Invalid or unsupported import material."));
        }
    }

    public Task<OperationResult<ExportedArtifact>> ExportCertificateAsync(ExportCertificateRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var certificate = X509CertificateLoader.LoadCertificate(request.CertificateDer);
        var artifact = request.Format switch
        {
            CryptoDataFormat.Pem => new ExportedArtifact(CryptoDataFormat.Pem, request.CertificateDer, certificate.ExportCertificatePem(), "application/x-pem-file", $"{request.FileNameStem}.pem"),
            CryptoDataFormat.Der => new ExportedArtifact(CryptoDataFormat.Der, request.CertificateDer, null, "application/pkix-cert", $"{request.FileNameStem}.cer"),
            _ => null
        };

        return artifact is null
            ? Task.FromResult(OperationResult<ExportedArtifact>.Failure(OperationErrorCode.ValidationFailed, "Unsupported certificate export format."))
            : Task.FromResult(OperationResult<ExportedArtifact>.Success(artifact, "Certificate exported."));
    }

    public Task<OperationResult<ExportedArtifact>> ExportCertificateSigningRequestAsync(ExportCertificateSigningRequestRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var artifact = request.Format switch
        {
            CryptoDataFormat.Pem => new ExportedArtifact(
                CryptoDataFormat.Pem,
                request.CertificateSigningRequestDer,
                PemEncoding.WriteString("CERTIFICATE REQUEST", request.CertificateSigningRequestDer),
                "application/x-pem-file",
                $"{request.FileNameStem}.csr.pem"),
            CryptoDataFormat.Pkcs10 or CryptoDataFormat.Der => new ExportedArtifact(
                request.Format,
                request.CertificateSigningRequestDer,
                null,
                "application/pkcs10",
                $"{request.FileNameStem}.csr"),
            _ => null
        };

        return artifact is null
            ? Task.FromResult(OperationResult<ExportedArtifact>.Failure(OperationErrorCode.ValidationFailed, "Unsupported CSR export format."))
            : Task.FromResult(OperationResult<ExportedArtifact>.Success(artifact, "Certificate signing request exported."));
    }

    public Task<OperationResult<ExportedArtifact>> ExportPkcs12Async(ExportPkcs12Request request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Task.FromResult(OperationResult<ExportedArtifact>.Failure(OperationErrorCode.ValidationFailed, "A password is required for PKCS#12 export."));
        }

        try
        {
            using var certificate = X509CertificateLoader.LoadCertificate(request.CertificateDer);
            using var loadedKey = LoadPrivateKey(request.PrivateKeyPkcs8, CryptoDataFormat.Pkcs8);
            using var certificateWithKey = AttachPrivateKey(certificate, loadedKey);
            var pfx = certificateWithKey.Export(X509ContentType.Pfx, request.Password);
            return Task.FromResult(OperationResult<ExportedArtifact>.Success(
                new ExportedArtifact(CryptoDataFormat.Pkcs12, pfx, null, "application/x-pkcs12", $"{request.FileNameStem}.pfx"),
                "PKCS#12 bundle exported."));
        }
        catch (CryptographicException)
        {
            return Task.FromResult(OperationResult<ExportedArtifact>.Failure(OperationErrorCode.ValidationFailed, "Failed to export the PKCS#12 bundle."));
        }
    }

    private static OperationResult<GenerateKeyPairResult> GenerateRsaKey(GenerateKeyPairRequest request)
    {
        var keySize = request.RsaKeySize ?? MinimumRsaKeySize;
        if (keySize < MinimumRsaKeySize)
        {
            return OperationResult<GenerateKeyPairResult>.Failure(OperationErrorCode.ValidationFailed, "RSA keys must be at least 3072 bits.");
        }

        using var rsa = RSA.Create(keySize);
        var pkcs8 = rsa.ExportPkcs8PrivateKey();
        var spki = rsa.ExportSubjectPublicKeyInfo();
        return OperationResult<GenerateKeyPairResult>.Success(
            new GenerateKeyPairResult(request.DisplayName, "RSA", ComputeFingerprint(spki), pkcs8, spki),
            "RSA key generated.");
    }

    private static OperationResult<GenerateKeyPairResult> GenerateEcdsaKey(GenerateKeyPairRequest request)
    {
        if (request.Curve is null)
        {
            return OperationResult<GenerateKeyPairResult>.Failure(OperationErrorCode.ValidationFailed, "An elliptic curve is required for ECDSA keys.");
        }

        using var ecdsa = ECDsa.Create(request.Curve == EllipticCurveKind.P384 ? ECCurve.NamedCurves.nistP384 : ECCurve.NamedCurves.nistP256);
        var pkcs8 = ecdsa.ExportPkcs8PrivateKey();
        var spki = ecdsa.ExportSubjectPublicKeyInfo();
        return OperationResult<GenerateKeyPairResult>.Success(
            new GenerateKeyPairResult(request.DisplayName, "ECDSA", ComputeFingerprint(spki), pkcs8, spki),
            "ECDSA key generated.");
    }

    private static ExportedArtifact ExportPrivateKeyPem(PrivateKeyExportRequest request, LoadedPrivateKey loadedKey)
    {
        string pem = string.IsNullOrWhiteSpace(request.Password)
            ? PemEncoding.WriteString("PRIVATE KEY", loadedKey.ExportPkcs8PrivateKey())
            : PemEncoding.WriteString("ENCRYPTED PRIVATE KEY", loadedKey.ExportEncryptedPkcs8PrivateKey(request.Password!));

        return new ExportedArtifact(CryptoDataFormat.Pem, loadedKey.ExportPkcs8PrivateKey(), pem, "application/x-pem-file", $"{request.DisplayName}.key.pem");
    }

    private static ExportedArtifact ExportPrivateKeyBinary(PrivateKeyExportRequest request, LoadedPrivateKey loadedKey)
    {
        var data = string.IsNullOrWhiteSpace(request.Password)
            ? loadedKey.ExportPkcs8PrivateKey()
            : loadedKey.ExportEncryptedPkcs8PrivateKey(request.Password!);

        return new ExportedArtifact(request.Format, data, null, "application/pkcs8", $"{request.DisplayName}.key");
    }

    private static CertificateRequest CreateCertificateRequest(string subjectName, LoadedPrivateKey loadedKey, IReadOnlyList<SanEntry> sans)
    {
        var distinguishedName = new X500DistinguishedName(subjectName);
        var certificateRequest = loadedKey.Algorithm switch
        {
            "RSA" => new CertificateRequest(distinguishedName, loadedKey.Rsa!, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
            "ECDSA" => new CertificateRequest(distinguishedName, loadedKey.Ecdsa!, HashAlgorithmName.SHA256),
            _ => throw new CryptographicException("Unsupported key algorithm.")
        };

        if (sans.Count > 0)
        {
            var builder = new SubjectAlternativeNameBuilder();
            foreach (var san in sans)
            {
                builder.AddDnsName(san.Value);
            }

            certificateRequest.CertificateExtensions.Add(builder.Build());
        }

        return certificateRequest;
    }

    private static LoadedPrivateKey LoadPrivateKey(byte[] data, CryptoDataFormat format)
    {
        return format switch
        {
            CryptoDataFormat.Pem => TryLoadPrivateKeyFromPem(data),
            CryptoDataFormat.Der or CryptoDataFormat.Pkcs8 => TryLoadPrivateKeyFromPkcs8(data),
            _ => throw new CryptographicException("Unsupported private key format.")
        };
    }

    private static LoadedPrivateKey TryLoadPrivateKeyFromPem(byte[] data)
    {
        var pem = System.Text.Encoding.UTF8.GetString(data);

        try
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            return LoadedPrivateKey.FromRsa(rsa);
        }
        catch (CryptographicException)
        {
        }

        var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(pem);
        return LoadedPrivateKey.FromEcdsa(ecdsa);
    }

    private static LoadedPrivateKey TryLoadPrivateKeyFromPkcs8(byte[] data)
    {
        try
        {
            var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(data, out _);
            return LoadedPrivateKey.FromRsa(rsa);
        }
        catch (CryptographicException)
        {
        }

        var ecdsa = ECDsa.Create();
        ecdsa.ImportPkcs8PrivateKey(data, out _);
        return LoadedPrivateKey.FromEcdsa(ecdsa);
    }

    private static X509Certificate2 LoadCertificate(byte[] data, CryptoDataFormat format)
    {
        return format switch
        {
            CryptoDataFormat.Pem => X509Certificate2.CreateFromPem(System.Text.Encoding.UTF8.GetString(data)),
            CryptoDataFormat.Der => X509CertificateLoader.LoadCertificate(data),
            _ => throw new CryptographicException("Unsupported certificate format.")
        };
    }

    private static CertificateDetails ParseCertificate(X509Certificate2 certificate)
    {
        var basicConstraints = certificate.Extensions.OfType<X509BasicConstraintsExtension>().SingleOrDefault();
        var keyUsage = certificate.Extensions.OfType<X509KeyUsageExtension>().SingleOrDefault();
        var eku = certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>().SingleOrDefault();
        var san = certificate.Extensions.Cast<X509Extension>().SingleOrDefault(x => x.Oid?.Value == "2.5.29.17");

        return new CertificateDetails(
            certificate.Subject,
            certificate.Issuer,
            certificate.SerialNumber,
            certificate.NotBefore,
            certificate.NotAfter,
            certificate.Thumbprint,
            Convert.ToHexString(SHA256.HashData(certificate.RawData)),
            GetPublicKeyAlgorithm(certificate.PublicKey.Oid?.Value),
            basicConstraints?.CertificateAuthority ?? false,
            keyUsage is null ? [] : keyUsage.KeyUsages.ToString().Split(", ", StringSplitOptions.RemoveEmptyEntries),
            eku is null ? [] : eku.EnhancedKeyUsages.Cast<Oid>().Select(x => x.FriendlyName ?? x.Value ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray(),
            ParseSubjectAlternativeNames(san?.RawData));
    }

    private static CertificateSigningRequestDetails ParseCertificateSigningRequest(CertificateRequest request)
    {
        var san = request.CertificateExtensions.SingleOrDefault(x => x.Oid?.Value == "2.5.29.17");
        return new CertificateSigningRequestDetails(
            request.SubjectName.Name ?? string.Empty,
            GetPublicKeyAlgorithm(request.PublicKey.Oid?.Value),
            ParseSubjectAlternativeNames(san?.RawData));
    }

    private static string[] ParseSubjectAlternativeNames(byte[]? rawData)
    {
        if (rawData is null || rawData.Length == 0)
        {
            return [];
        }

        var reader = new AsnReader(rawData, AsnEncodingRules.DER);
        var sequence = reader.ReadSequence();
        var names = new List<string>();

        while (sequence.HasData)
        {
            var tag = sequence.PeekTag();
            if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 2)))
            {
                names.Add(sequence.ReadCharacterString(UniversalTagNumber.IA5String, new Asn1Tag(TagClass.ContextSpecific, 2)));
            }
            else
            {
                sequence.ReadEncodedValue();
            }
        }

        return names.ToArray();
    }

    private static string GetPublicKeyAlgorithm(string? oidValue)
    {
        return oidValue switch
        {
            "1.2.840.113549.1.1.1" => "RSA",
            "1.2.840.10045.2.1" => "ECDSA",
            _ => oidValue ?? "Unknown"
        };
    }

    private static string ComputeFingerprint(byte[] subjectPublicKeyInfo)
    {
        return Convert.ToHexString(SHA256.HashData(subjectPublicKeyInfo));
    }

    private static byte[] BuildCertificateRevocationList(
        CertificateRevocationListBuilder builder,
        X509Certificate2 issuerCertificateWithKey,
        long crlNumber,
        DateTimeOffset thisUpdate,
        DateTimeOffset nextUpdate,
        string algorithm)
    {
        return algorithm switch
        {
            "RSA" => builder.Build(
                issuerCertificateWithKey,
                new BigInteger(crlNumber),
                nextUpdate,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1,
                thisUpdate),
            "ECDSA" => builder.Build(
                issuerCertificateWithKey,
                new BigInteger(crlNumber),
                nextUpdate,
                HashAlgorithmName.SHA256,
                null,
                thisUpdate),
            _ => throw new CryptographicException("Unsupported issuer key algorithm.")
        };
    }

    private static CertificateRevocationListDetails ParseCertificateRevocationList(byte[] data, CryptoDataFormat format)
    {
        var derData = format == CryptoDataFormat.Pem ? DecodePem("X509 CRL", data) : data;
        var reader = new AsnReader(derData, AsnEncodingRules.DER);
        var certificateList = reader.ReadSequence();
        var tbsCertList = certificateList.ReadSequence();

        if (tbsCertList.PeekTag().HasSameClassAndValue(Asn1Tag.Integer))
        {
            tbsCertList.ReadInteger();
        }

        ReadAlgorithmIdentifier(tbsCertList);
        var issuerNameBytes = tbsCertList.ReadEncodedValue().ToArray();
        var issuer = new X500DistinguishedName(issuerNameBytes).Name ?? string.Empty;
        var thisUpdate = ReadTime(tbsCertList);
        DateTimeOffset? nextUpdate = null;
        if (tbsCertList.HasData && IsTimeTag(tbsCertList.PeekTag()))
        {
            nextUpdate = ReadTime(tbsCertList);
        }

        var revokedEntries = new List<RevokedCertificateEntry>();
        if (tbsCertList.HasData && tbsCertList.PeekTag().HasSameClassAndValue(Asn1Tag.Sequence))
        {
            var revokedCertificates = tbsCertList.ReadSequence();
            while (revokedCertificates.HasData)
            {
                var revokedEntry = revokedCertificates.ReadSequence();
                var serialNumber = Convert.ToHexString(revokedEntry.ReadIntegerBytes().ToArray());
                var revokedAt = ReadTime(revokedEntry);
                var reason = CertificateRevocationReason.Unspecified;

                if (revokedEntry.HasData)
                {
                    var extensions = revokedEntry.ReadSequence();
                    while (extensions.HasData)
                    {
                        var extension = extensions.ReadSequence();
                        var oid = extension.ReadObjectIdentifier();
                        if (extension.HasData && extension.PeekTag().HasSameClassAndValue(Asn1Tag.Boolean))
                        {
                            extension.ReadBoolean();
                        }

                        var value = extension.ReadOctetString();
                        if (oid == "2.5.29.21")
                        {
                            var reasonReader = new AsnReader(value, AsnEncodingRules.DER);
                            reason = reasonReader.ReadEnumeratedValue<CertificateRevocationReason>();
                        }
                    }
                }

                revokedEntries.Add(new RevokedCertificateEntry(Guid.Empty, serialNumber, serialNumber, serialNumber, reason, revokedAt));
            }
        }

        string crlNumber = "0";
        if (tbsCertList.HasData && tbsCertList.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
        {
            var extensionsContainer = tbsCertList.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
            var extensions = extensionsContainer.ReadSequence();
            while (extensions.HasData)
            {
                var extension = extensions.ReadSequence();
                var oid = extension.ReadObjectIdentifier();
                if (extension.HasData && extension.PeekTag().HasSameClassAndValue(Asn1Tag.Boolean))
                {
                    extension.ReadBoolean();
                }

                var value = extension.ReadOctetString();
                if (oid == "2.5.29.20")
                {
                    var numberReader = new AsnReader(value, AsnEncodingRules.DER);
                    crlNumber = numberReader.ReadInteger().ToString();
                }
            }
        }

        return new CertificateRevocationListDetails(issuer, crlNumber, thisUpdate, nextUpdate, revokedEntries);
    }

    private static ReadOnlyMemory<byte> ReadAlgorithmIdentifier(AsnReader reader)
    {
        return reader.ReadEncodedValue();
    }

    private static DateTimeOffset ReadTime(AsnReader reader)
    {
        var tag = reader.PeekTag();
        return tag.TagValue switch
        {
            (int)UniversalTagNumber.UtcTime => reader.ReadUtcTime(),
            (int)UniversalTagNumber.GeneralizedTime => reader.ReadGeneralizedTime(),
            _ => throw new CryptographicException("Unsupported time value in CRL.")
        };
    }

    private static bool IsTimeTag(Asn1Tag tag)
    {
        return tag.TagClass == TagClass.Universal
            && (tag.TagValue == (int)UniversalTagNumber.UtcTime || tag.TagValue == (int)UniversalTagNumber.GeneralizedTime);
    }

    private static byte[] DecodePem(string label, byte[] data)
    {
        var text = System.Text.Encoding.UTF8.GetString(data);
        var field = PemEncoding.Find(text);
        if (!text[field.Label].SequenceEqual(label))
        {
            throw new CryptographicException("PEM payload not found.");
        }

        return Convert.FromBase64String(text[field.Base64Data].ToString());
    }

    private static byte[] ParseSerialNumber(string serialNumber)
    {
        return Convert.FromHexString(serialNumber.Length % 2 == 0 ? serialNumber : $"0{serialNumber}");
    }

    private static X509RevocationReason? MapRevocationReason(CertificateRevocationReason reason)
    {
        return reason switch
        {
            CertificateRevocationReason.KeyCompromise => X509RevocationReason.KeyCompromise,
            CertificateRevocationReason.CaCompromise => X509RevocationReason.CACompromise,
            CertificateRevocationReason.AffiliationChanged => X509RevocationReason.AffiliationChanged,
            CertificateRevocationReason.Superseded => X509RevocationReason.Superseded,
            CertificateRevocationReason.CessationOfOperation => X509RevocationReason.CessationOfOperation,
            CertificateRevocationReason.CertificateHold => X509RevocationReason.CertificateHold,
            CertificateRevocationReason.PrivilegeWithdrawn => X509RevocationReason.PrivilegeWithdrawn,
            CertificateRevocationReason.AaCompromise => X509RevocationReason.AACompromise,
            _ => X509RevocationReason.Unspecified
        };
    }

    private static CertificateRevocationReason MapRevocationReason(int reason)
    {
        return Enum.IsDefined(typeof(CertificateRevocationReason), reason)
            ? (CertificateRevocationReason)reason
            : CertificateRevocationReason.Unspecified;
    }

    private static X509SignatureGenerator CreateSignatureGenerator(LoadedPrivateKey loadedKey)
    {
        return loadedKey.Algorithm switch
        {
            "RSA" => X509SignatureGenerator.CreateForRSA(loadedKey.Rsa!, RSASignaturePadding.Pkcs1),
            "ECDSA" => X509SignatureGenerator.CreateForECDsa(loadedKey.Ecdsa!),
            _ => throw new CryptographicException("Unsupported issuer key algorithm.")
        };
    }

    private static X509Certificate2 AttachPrivateKey(X509Certificate2 certificate, LoadedPrivateKey loadedKey)
    {
        return loadedKey.Algorithm switch
        {
            "RSA" => certificate.CopyWithPrivateKey(loadedKey.Rsa!),
            "ECDSA" => certificate.CopyWithPrivateKey(loadedKey.Ecdsa!),
            _ => throw new CryptographicException("Unsupported private key algorithm.")
        };
    }

    private Task<OperationResult<ImportCertificateMaterialResult>> ImportPrivateKeyMaterial(ImportCertificateMaterialRequest request)
    {
        return ImportPrivateKeyAsync(new PrivateKeyImportRequest(request.Data, request.Format, request.DisplayName, request.Password), CancellationToken.None)
            .ContinueWith(task =>
            {
                var result = task.Result;
                return result.IsSuccess && result.Value is not null
                    ? OperationResult<ImportCertificateMaterialResult>.Success(
                        new ImportCertificateMaterialResult(
                            [new ImportedPrivateKeyMaterial(result.Value.DisplayName, result.Value.Algorithm, result.Value.PublicKeyFingerprint, result.Value.Pkcs8PrivateKey)],
                            [],
                            []),
                        "Private key material imported.")
                    : OperationResult<ImportCertificateMaterialResult>.Failure(result.ErrorCode, result.Message);
            });
    }

    private Task<OperationResult<ImportCertificateMaterialResult>> ImportCertificateMaterial(ImportCertificateMaterialRequest request)
    {
        using var certificate = request.Format == CryptoDataFormat.Pem
            ? X509Certificate2.CreateFromPem(System.Text.Encoding.UTF8.GetString(request.Data))
            : X509CertificateLoader.LoadCertificate(request.Data);

        return Task.FromResult(OperationResult<ImportCertificateMaterialResult>.Success(
            new ImportCertificateMaterialResult(
                [],
                [new ImportedCertificateMaterial(request.DisplayName, certificate.Export(X509ContentType.Cert), ParseCertificate(certificate))],
                []),
            "Certificate material imported."));
    }

    private Task<OperationResult<ImportCertificateMaterialResult>> ImportCertificateSigningRequestMaterial(ImportCertificateMaterialRequest request)
    {
        return ParseAsync(new CertificateSigningRequestParseRequest(request.Data, request.Format), CancellationToken.None)
            .ContinueWith(task =>
            {
                var result = task.Result;
                return result.IsSuccess && result.Value is not null
                    ? OperationResult<ImportCertificateMaterialResult>.Success(
                        new ImportCertificateMaterialResult(
                            [],
                            [],
                            [new ImportedCertificateSigningRequestMaterial(request.DisplayName, request.Data, result.Value)]),
                        "CSR material imported.")
                    : OperationResult<ImportCertificateMaterialResult>.Failure(result.ErrorCode, result.Message);
            });
    }

    private Task<OperationResult<ImportCertificateMaterialResult>> ImportBundleMaterial(ImportCertificateMaterialRequest request)
    {
        if (request.Format != CryptoDataFormat.Pkcs12)
        {
            return Task.FromResult(OperationResult<ImportCertificateMaterialResult>.Failure(OperationErrorCode.ValidationFailed, "Bundle import currently supports PKCS#12 only."));
        }

        using var certificate = X509CertificateLoader.LoadPkcs12(request.Data, request.Password, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
        var certificates = new List<ImportedCertificateMaterial>
        {
            new(request.DisplayName, certificate.Export(X509ContentType.Cert), ParseCertificate(certificate))
        };

        var privateKeys = new List<ImportedPrivateKeyMaterial>();
        if (certificate.HasPrivateKey)
        {
            using var loadedKey = certificate.GetRSAPrivateKey() is RSA rsa
                ? LoadedPrivateKey.FromRsa(rsa)
                : LoadedPrivateKey.FromEcdsa(certificate.GetECDsaPrivateKey() ?? throw new CryptographicException("Unsupported private key algorithm."));

            privateKeys.Add(new ImportedPrivateKeyMaterial(
                $"{request.DisplayName} Key",
                loadedKey.Algorithm,
                ComputeFingerprint(loadedKey.ExportSubjectPublicKeyInfo()),
                loadedKey.ExportPkcs8PrivateKey()));
        }

        return Task.FromResult(OperationResult<ImportCertificateMaterialResult>.Success(
            new ImportCertificateMaterialResult(privateKeys, certificates, []),
            "Bundle material imported."));
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

        public byte[] ExportPkcs8PrivateKey() => Rsa?.ExportPkcs8PrivateKey() ?? Ecdsa!.ExportPkcs8PrivateKey();

        public byte[] ExportEncryptedPkcs8PrivateKey(string password)
        {
            var pbe = new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 200_000);
            return Rsa?.ExportEncryptedPkcs8PrivateKey(password, pbe) ?? Ecdsa!.ExportEncryptedPkcs8PrivateKey(password, pbe);
        }

        public byte[] ExportSubjectPublicKeyInfo() => Rsa?.ExportSubjectPublicKeyInfo() ?? Ecdsa!.ExportSubjectPublicKeyInfo();

        public void Dispose()
        {
            Rsa?.Dispose();
            Ecdsa?.Dispose();
        }
    }
}
