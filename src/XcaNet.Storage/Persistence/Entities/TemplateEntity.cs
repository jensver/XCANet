namespace XcaNet.Storage.Persistence.Entities;

public sealed class TemplateEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsEnabled { get; set; }
    public string IntendedUsage { get; set; } = string.Empty;
    public string? SubjectDefault { get; set; }
    public string SubjectAlternativeNames { get; set; } = string.Empty;
    public string KeyAlgorithm { get; set; } = string.Empty;
    public int? RsaKeySize { get; set; }
    public string? Curve { get; set; }
    public string SignatureAlgorithm { get; set; } = string.Empty;
    public int ValidityDays { get; set; }
    public bool IsCertificateAuthority { get; set; }
    public int? PathLengthConstraint { get; set; }
    public string KeyUsages { get; set; } = string.Empty;
    public string EnhancedKeyUsages { get; set; } = string.Empty;
    public string? Comment { get; set; }
}
