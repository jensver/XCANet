using XcaNet.Contracts.Browser;
using XcaNet.Contracts.Crypto;
using XcaNet.Storage.Persistence.Entities;

namespace XcaNet.Application.Templates;

internal static class TemplateModelMapper
{
    private static readonly IReadOnlyDictionary<string, string> KnownEnhancedKeyUsages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Server Authentication"] = "Server Authentication",
        ["Client Authentication"] = "Client Authentication",
        ["Code Signing"] = "Code Signing",
        ["Email Protection"] = "Email Protection",
        ["Time Stamping"] = "Time Stamping",
        ["OCSP Signing"] = "OCSP Signing"
    };

    private static readonly HashSet<string> KnownKeyUsages = new(StringComparer.OrdinalIgnoreCase)
    {
        "DigitalSignature",
        "KeyEncipherment",
        "DataEncipherment",
        "KeyAgreement",
        "KeyCertSign",
        "CrlSign"
    };

    public static TemplateEntity ToEntity(SaveTemplateRequest request, Guid templateId)
    {
        return new TemplateEntity
        {
            Id = templateId,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsFavorite = request.IsFavorite,
            IsEnabled = request.IsEnabled,
            IntendedUsage = request.IntendedUsage.ToString(),
            SubjectDefault = string.IsNullOrWhiteSpace(request.SubjectDefault) ? null : request.SubjectDefault.Trim(),
            SubjectAlternativeNames = Join(request.SubjectAlternativeNames),
            KeyAlgorithm = request.KeyAlgorithm.ToString(),
            RsaKeySize = request.RsaKeySize,
            Curve = request.Curve?.ToString(),
            SignatureAlgorithm = request.SignatureAlgorithm.Trim(),
            ValidityDays = request.ValidityDays,
            IsCertificateAuthority = request.IsCertificateAuthority,
            PathLengthConstraint = request.PathLengthConstraint,
            KeyUsages = Join(request.KeyUsages),
            EnhancedKeyUsages = Join(request.EnhancedKeyUsages)
        };
    }

    public static TemplateDetails ToDetails(TemplateEntity entity)
    {
        var validation = Validate(entity);
        var sanDefaults = Split(entity.SubjectAlternativeNames);
        var keyUsages = Split(entity.KeyUsages);
        var enhancedKeyUsages = Split(entity.EnhancedKeyUsages);
        return new TemplateDetails(
            entity.Id,
            entity.Name,
            entity.Description,
            entity.IsFavorite,
            entity.IsEnabled,
            ParseIntendedUsage(entity.IntendedUsage),
            entity.SubjectDefault,
            sanDefaults,
            ParseKeyAlgorithm(entity.KeyAlgorithm),
            entity.RsaKeySize,
            ParseCurve(entity.Curve),
            string.IsNullOrWhiteSpace(entity.SignatureAlgorithm) ? "SHA-256" : entity.SignatureAlgorithm,
            entity.ValidityDays,
            entity.IsCertificateAuthority,
            entity.PathLengthConstraint,
            keyUsages,
            enhancedKeyUsages,
            CreatePreview(entity, sanDefaults, keyUsages, enhancedKeyUsages),
            validation);
    }

    public static TemplateListItem ToListItem(TemplateEntity entity)
    {
        var details = ToDetails(entity);
        return new TemplateListItem(
            details.TemplateId,
            details.Name,
            details.Description,
            details.IntendedUsage,
            details.IsFavorite,
            details.IsEnabled,
            $"{details.Preview.KeySummary} | {details.Preview.ValiditySummary}");
    }

    public static AppliedTemplateDefaults ToAppliedDefaults(TemplateEntity entity, TemplateWorkflowKind workflow)
    {
        var details = ToDetails(entity);
        return new AppliedTemplateDefaults(
            details.TemplateId,
            details.Name,
            workflow,
            details.SubjectDefault,
            details.SubjectAlternativeNames,
            details.KeyAlgorithm,
            details.RsaKeySize,
            details.Curve,
            details.SignatureAlgorithm,
            details.ValidityDays,
            details.IsCertificateAuthority,
            details.PathLengthConstraint,
            details.KeyUsages,
            details.EnhancedKeyUsages,
            details.Preview,
            details.Validation);
    }

    public static TemplateValidationSummary Validate(TemplateEntity entity)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var intendedUsage = ParseIntendedUsage(entity.IntendedUsage);
        var keyAlgorithm = ParseKeyAlgorithm(entity.KeyAlgorithm);
        var keyUsages = Split(entity.KeyUsages);
        var enhancedKeyUsages = Split(entity.EnhancedKeyUsages);

        if (string.IsNullOrWhiteSpace(entity.Name))
        {
            errors.Add("Template name is required.");
        }

        if (entity.ValidityDays <= 0)
        {
            errors.Add("Validity days must be greater than zero.");
        }

        if (!string.Equals(entity.SignatureAlgorithm, "SHA-256", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Only SHA-256 is currently supported for template signature defaults.");
        }

        if (keyAlgorithm == KeyAlgorithmKind.Rsa)
        {
            if (entity.RsaKeySize is null || entity.RsaKeySize < 3072)
            {
                errors.Add("RSA templates require a key size of at least 3072 bits.");
            }

            if (!string.IsNullOrWhiteSpace(entity.Curve))
            {
                errors.Add("RSA templates cannot define an elliptic-curve default.");
            }
        }
        else
        {
            if (entity.RsaKeySize is not null)
            {
                errors.Add("ECDSA templates cannot define an RSA key size.");
            }

            if (ParseCurve(entity.Curve) is null)
            {
                errors.Add("ECDSA templates require a supported curve.");
            }
        }

        if ((intendedUsage is TemplateIntendedUsage.SelfSignedCa or TemplateIntendedUsage.IntermediateCa) && !entity.IsCertificateAuthority)
        {
            errors.Add("CA templates must enable certificate-authority basic constraints.");
        }

        if ((intendedUsage is TemplateIntendedUsage.EndEntityCertificate or TemplateIntendedUsage.CertificateSigningRequest) && entity.IsCertificateAuthority)
        {
            errors.Add("End-entity and CSR templates cannot be marked as certificate authorities.");
        }

        if (!entity.IsCertificateAuthority && keyUsages.Any(x => x.Equals("KeyCertSign", StringComparison.OrdinalIgnoreCase) || x.Equals("CrlSign", StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add("Non-CA templates cannot request KeyCertSign or CrlSign usage.");
        }

        if (entity.IsCertificateAuthority && !keyUsages.Any(x => x.Equals("KeyCertSign", StringComparison.OrdinalIgnoreCase)))
        {
            warnings.Add("CA templates usually include KeyCertSign.");
        }

        if (intendedUsage == TemplateIntendedUsage.CertificateSigningRequest && entity.ValidityDays > 0)
        {
            warnings.Add("CSR templates store a validity default for later issuance, but the CSR itself does not contain certificate validity.");
        }

        if (intendedUsage == TemplateIntendedUsage.CertificateSigningRequest && entity.IsCertificateAuthority)
        {
            errors.Add("Generic CSR templates should not carry issuance-time CA assumptions.");
        }

        foreach (var keyUsage in keyUsages)
        {
            if (!KnownKeyUsages.Contains(keyUsage))
            {
                errors.Add($"Unsupported key usage '{keyUsage}'.");
            }
        }

        foreach (var enhancedKeyUsage in enhancedKeyUsages)
        {
            if (!KnownEnhancedKeyUsages.ContainsKey(enhancedKeyUsage))
            {
                errors.Add($"Unsupported enhanced key usage '{enhancedKeyUsage}'.");
            }
        }

        return new TemplateValidationSummary(errors, warnings);
    }

    public static string? ValidateWorkflowCompatibility(TemplateEntity entity, TemplateWorkflowKind workflow)
    {
        if (!entity.IsEnabled)
        {
            return "Disabled templates cannot be used in issuance workflows.";
        }

        var usage = ParseIntendedUsage(entity.IntendedUsage);
        return workflow switch
        {
            TemplateWorkflowKind.GenerateKey => null,
            TemplateWorkflowKind.SelfSignedCa when usage != TemplateIntendedUsage.SelfSignedCa => "Only self-signed CA templates can be used for self-signed CA creation.",
            TemplateWorkflowKind.CertificateSigningRequest when usage == TemplateIntendedUsage.SelfSignedCa => "Self-signed CA templates cannot be used for CSR creation.",
            TemplateWorkflowKind.SignCertificateSigningRequest when usage is TemplateIntendedUsage.SelfSignedCa or TemplateIntendedUsage.CertificateSigningRequest => "Selected template is not compatible with certificate issuance from a CSR.",
            _ => null
        };
    }

    private static TemplatePreviewSummary CreatePreview(TemplateEntity entity, IReadOnlyList<string> sanDefaults, IReadOnlyList<string> keyUsages, IReadOnlyList<string> enhancedKeyUsages)
    {
        var keySummary = ParseKeyAlgorithm(entity.KeyAlgorithm) == KeyAlgorithmKind.Rsa
            ? $"RSA {entity.RsaKeySize ?? 3072}"
            : $"ECDSA {ParseCurve(entity.Curve)?.ToString() ?? "P256"}";

        var extensionSummary = $"{(entity.IsCertificateAuthority ? "CA" : "Leaf")} | KU: {(keyUsages.Count == 0 ? "default" : string.Join(", ", keyUsages))}";
        if (enhancedKeyUsages.Count > 0)
        {
            extensionSummary += $" | EKU: {string.Join(", ", enhancedKeyUsages)}";
        }

        return new TemplatePreviewSummary(
            ParseIntendedUsage(entity.IntendedUsage).ToString(),
            string.IsNullOrWhiteSpace(entity.SubjectDefault) ? "Subject not preset" : entity.SubjectDefault!,
            sanDefaults.Count == 0 ? "No SAN defaults" : string.Join(", ", sanDefaults),
            keySummary,
            $"{entity.ValidityDays} day(s)",
            extensionSummary,
            $"{(entity.IsEnabled ? "Enabled" : "Disabled")} | {(entity.IsFavorite ? "Favorite" : "Standard")}");
    }

    private static KeyAlgorithmKind ParseKeyAlgorithm(string value)
        => Enum.TryParse<KeyAlgorithmKind>(value, true, out var algorithm) ? algorithm : KeyAlgorithmKind.Rsa;

    private static EllipticCurveKind? ParseCurve(string? value)
        => Enum.TryParse<EllipticCurveKind>(value, true, out var curve) ? curve : null;

    private static TemplateIntendedUsage ParseIntendedUsage(string value)
        => Enum.TryParse<TemplateIntendedUsage>(value, true, out var intendedUsage) ? intendedUsage : TemplateIntendedUsage.EndEntityCertificate;

    private static IReadOnlyList<string> Split(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private static string Join(IEnumerable<string> values)
        => string.Join(';', values.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
}
