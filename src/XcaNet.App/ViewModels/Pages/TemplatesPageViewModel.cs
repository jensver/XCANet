using System.Windows.Input;
using XcaNet.Contracts.Browser;
using XcaNet.Contracts.Crypto;

namespace XcaNet.App.ViewModels.Pages;

public sealed class TemplatesPageViewModel : SelectableItemsPageViewModelBase<TemplateListItem, Guid>
{
    private readonly List<TemplateListItem> _allItems = [];

    private TemplateStatusFilterView _statusFilter = TemplateStatusFilterView.EnabledOnly;
    private TemplateUsageFilterView _usageFilter = TemplateUsageFilterView.All;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private bool _isFavorite;
    private bool _isEnabled = true;
    private TemplateIntendedUsage _intendedUsage = TemplateIntendedUsage.EndEntityCertificate;
    private string _subjectDefault = string.Empty;
    private string _subjectAlternativeNames = string.Empty;
    private KeyAlgorithmKind _keyAlgorithm = KeyAlgorithmKind.Rsa;
    private int _rsaKeySize = 3072;
    private EllipticCurveKind _curve = EllipticCurveKind.P256;
    private string _signatureAlgorithm = "SHA-256";
    private int _validityDays = 365;
    private bool _isCertificateAuthority;
    private int _pathLengthConstraint = 0;
    private bool _hasPathLengthConstraint;
    private string _keyUsages = "DigitalSignature";
    private string _enhancedKeyUsages = "Server Authentication";
    private string _validationSummary = string.Empty;
    private string _previewSummary = "Template preview will appear here.";

    public TemplatesPageViewModel()
        : base("Templates")
    {
        EmptyStateTitle = "No templates saved";
        EmptyStateMessage = "Create a template to speed up CA, CSR, and issuance workflows.";
        UpdateUsageDefaults();
        UpdatePreview();
    }

    public IReadOnlyList<TemplateIntendedUsage> IntendedUsages { get; } =
    [
        TemplateIntendedUsage.SelfSignedCa,
        TemplateIntendedUsage.IntermediateCa,
        TemplateIntendedUsage.EndEntityCertificate,
        TemplateIntendedUsage.CertificateSigningRequest
    ];

    public IReadOnlyList<KeyAlgorithmKind> KeyAlgorithms { get; } = [KeyAlgorithmKind.Rsa, KeyAlgorithmKind.Ecdsa];

    public IReadOnlyList<EllipticCurveKind> Curves { get; } = [EllipticCurveKind.P256, EllipticCurveKind.P384];

    public IReadOnlyList<TemplateStatusFilterView> StatusFilters { get; } = [TemplateStatusFilterView.EnabledOnly, TemplateStatusFilterView.DisabledOnly, TemplateStatusFilterView.All];

    public IReadOnlyList<TemplateUsageFilterView> UsageFilters { get; } =
    [
        TemplateUsageFilterView.All,
        TemplateUsageFilterView.SelfSignedCa,
        TemplateUsageFilterView.IntermediateCa,
        TemplateUsageFilterView.EndEntityCertificate,
        TemplateUsageFilterView.CertificateSigningRequest
    ];

    public TemplateStatusFilterView StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetProperty(ref _statusFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    public TemplateUsageFilterView UsageFilter
    {
        get => _usageFilter;
        set
        {
            if (SetProperty(ref _usageFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                UpdatePreview();
            }
        }
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public bool IsFavorite
    {
        get => _isFavorite;
        set
        {
            if (SetProperty(ref _isFavorite, value))
            {
                UpdatePreview();
            }
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetProperty(ref _isEnabled, value))
            {
                UpdatePreview();
            }
        }
    }

    public TemplateIntendedUsage IntendedUsage
    {
        get => _intendedUsage;
        set
        {
            if (SetProperty(ref _intendedUsage, value))
            {
                UpdateUsageDefaults();
                UpdatePreview();
            }
        }
    }

    public string SubjectDefault
    {
        get => _subjectDefault;
        set
        {
            if (SetProperty(ref _subjectDefault, value))
            {
                UpdatePreview();
            }
        }
    }

    public string SubjectAlternativeNames
    {
        get => _subjectAlternativeNames;
        set
        {
            if (SetProperty(ref _subjectAlternativeNames, value))
            {
                UpdatePreview();
            }
        }
    }

    public KeyAlgorithmKind KeyAlgorithm
    {
        get => _keyAlgorithm;
        set
        {
            if (SetProperty(ref _keyAlgorithm, value))
            {
                UpdatePreview();
            }
        }
    }

    public int RsaKeySize
    {
        get => _rsaKeySize;
        set
        {
            if (SetProperty(ref _rsaKeySize, value))
            {
                UpdatePreview();
            }
        }
    }

    public EllipticCurveKind Curve
    {
        get => _curve;
        set
        {
            if (SetProperty(ref _curve, value))
            {
                UpdatePreview();
            }
        }
    }

    public string SignatureAlgorithm
    {
        get => _signatureAlgorithm;
        set => SetProperty(ref _signatureAlgorithm, value);
    }

    public int ValidityDays
    {
        get => _validityDays;
        set
        {
            if (SetProperty(ref _validityDays, value))
            {
                UpdatePreview();
            }
        }
    }

    public bool IsCertificateAuthority
    {
        get => _isCertificateAuthority;
        set
        {
            if (SetProperty(ref _isCertificateAuthority, value))
            {
                UpdatePreview();
            }
        }
    }

    public int PathLengthConstraint
    {
        get => _pathLengthConstraint;
        set => SetProperty(ref _pathLengthConstraint, value);
    }

    public bool HasPathLengthConstraint
    {
        get => _hasPathLengthConstraint;
        set => SetProperty(ref _hasPathLengthConstraint, value);
    }

    public string KeyUsages
    {
        get => _keyUsages;
        set
        {
            if (SetProperty(ref _keyUsages, value))
            {
                UpdatePreview();
            }
        }
    }

    public string EnhancedKeyUsages
    {
        get => _enhancedKeyUsages;
        set
        {
            if (SetProperty(ref _enhancedKeyUsages, value))
            {
                UpdatePreview();
            }
        }
    }

    public string ValidationSummary
    {
        get => _validationSummary;
        set => SetProperty(ref _validationSummary, value);
    }

    public string PreviewSummary
    {
        get => _previewSummary;
        private set => SetProperty(ref _previewSummary, value);
    }

    public bool IsEditingExisting => SelectedItem is not null;

    public ICommand? CreateNewCommand { get; set; }

    public ICommand? SaveTemplateCommand { get; set; }

    public ICommand? CloneTemplateCommand { get; set; }

    public ICommand? ToggleFavoriteCommand { get; set; }

    public ICommand? ToggleEnabledCommand { get; set; }

    public ICommand? DeleteTemplateCommand { get; set; }

    protected override Guid GetItemId(TemplateListItem item) => item.TemplateId;

    public void SetTemplates(IEnumerable<TemplateListItem> items)
    {
        _allItems.Clear();
        _allItems.AddRange(items);
        ApplyFilters();
    }

    public void LoadTemplate(TemplateDetails template)
    {
        Name = template.Name;
        Description = template.Description ?? string.Empty;
        IsFavorite = template.IsFavorite;
        IsEnabled = template.IsEnabled;
        IntendedUsage = template.IntendedUsage;
        SubjectDefault = template.SubjectDefault ?? string.Empty;
        SubjectAlternativeNames = string.Join(", ", template.SubjectAlternativeNames);
        KeyAlgorithm = template.KeyAlgorithm;
        RsaKeySize = template.RsaKeySize ?? 3072;
        Curve = template.Curve ?? EllipticCurveKind.P256;
        SignatureAlgorithm = template.SignatureAlgorithm;
        ValidityDays = template.ValidityDays;
        IsCertificateAuthority = template.IsCertificateAuthority;
        HasPathLengthConstraint = template.PathLengthConstraint.HasValue;
        PathLengthConstraint = template.PathLengthConstraint ?? 0;
        KeyUsages = string.Join(", ", template.KeyUsages);
        EnhancedKeyUsages = string.Join(", ", template.EnhancedKeyUsages);
        ValidationSummary = BuildValidationSummary(template.Validation);
        PreviewSummary = string.Join(Environment.NewLine,
        [
            template.Preview.UsageSummary,
            template.Preview.SubjectSummary,
            template.Preview.SanSummary,
            template.Preview.KeySummary,
            template.Preview.ValiditySummary,
            template.Preview.ExtensionSummary,
            template.Preview.StateSummary
        ]);
        OnPropertyChanged(nameof(IsEditingExisting));
    }

    public void PrepareNewTemplate()
    {
        SelectedItem = null;
        Name = string.Empty;
        Description = string.Empty;
        IsFavorite = false;
        IsEnabled = true;
        IntendedUsage = TemplateIntendedUsage.EndEntityCertificate;
        SubjectDefault = string.Empty;
        SubjectAlternativeNames = string.Empty;
        KeyAlgorithm = KeyAlgorithmKind.Rsa;
        RsaKeySize = 3072;
        Curve = EllipticCurveKind.P256;
        SignatureAlgorithm = "SHA-256";
        ValidityDays = 365;
        HasPathLengthConstraint = false;
        PathLengthConstraint = 0;
        ValidationSummary = string.Empty;
        UpdateUsageDefaults();
        UpdatePreview();
        OnPropertyChanged(nameof(IsEditingExisting));
    }

    public SaveTemplateRequest BuildSaveRequest()
    {
        return new SaveTemplateRequest(
            SelectedItem?.TemplateId,
            Name,
            Description,
            IsFavorite,
            IsEnabled,
            IntendedUsage,
            SubjectDefault,
            SplitValues(SubjectAlternativeNames),
            KeyAlgorithm,
            KeyAlgorithm == KeyAlgorithmKind.Rsa ? RsaKeySize : null,
            KeyAlgorithm == KeyAlgorithmKind.Ecdsa ? Curve : null,
            SignatureAlgorithm,
            ValidityDays,
            IsCertificateAuthority,
            HasPathLengthConstraint ? PathLengthConstraint : null,
            SplitValues(KeyUsages),
            SplitValues(EnhancedKeyUsages));
    }

    protected override void OnItemsChanged()
    {
        base.OnItemsChanged();
        OnPropertyChanged(nameof(IsEditingExisting));
    }

    private void ApplyFilters()
    {
        var filtered = _allItems
            .Where(x => StatusFilter switch
            {
                TemplateStatusFilterView.EnabledOnly => x.IsEnabled,
                TemplateStatusFilterView.DisabledOnly => !x.IsEnabled,
                _ => true
            })
            .Where(x => UsageFilter switch
            {
                TemplateUsageFilterView.SelfSignedCa => x.IntendedUsage == TemplateIntendedUsage.SelfSignedCa,
                TemplateUsageFilterView.IntermediateCa => x.IntendedUsage == TemplateIntendedUsage.IntermediateCa,
                TemplateUsageFilterView.EndEntityCertificate => x.IntendedUsage == TemplateIntendedUsage.EndEntityCertificate,
                TemplateUsageFilterView.CertificateSigningRequest => x.IntendedUsage == TemplateIntendedUsage.CertificateSigningRequest,
                _ => true
            });

        SetItems(filtered);
    }

    private void UpdateUsageDefaults()
    {
        if (IntendedUsage is TemplateIntendedUsage.SelfSignedCa or TemplateIntendedUsage.IntermediateCa)
        {
            IsCertificateAuthority = true;
            KeyUsages = "KeyCertSign, CrlSign, DigitalSignature";
            EnhancedKeyUsages = string.Empty;
            ValidityDays = Math.Max(ValidityDays, 3650);
        }
        else
        {
            IsCertificateAuthority = false;
            KeyUsages = string.IsNullOrWhiteSpace(KeyUsages) || KeyUsages.Contains("KeyCertSign", StringComparison.OrdinalIgnoreCase)
                ? "DigitalSignature, KeyEncipherment"
                : KeyUsages;
            EnhancedKeyUsages = string.IsNullOrWhiteSpace(EnhancedKeyUsages) ? "Server Authentication" : EnhancedKeyUsages;
            ValidityDays = Math.Max(ValidityDays, 365);
        }
    }

    private void UpdatePreview()
    {
        var keySummary = KeyAlgorithm == KeyAlgorithmKind.Rsa ? $"RSA {RsaKeySize}" : $"ECDSA {Curve}";
        PreviewSummary = string.Join(
            Environment.NewLine,
            [
                $"Usage: {IntendedUsage}",
                $"Subject: {(string.IsNullOrWhiteSpace(SubjectDefault) ? "not preset" : SubjectDefault)}",
                $"SAN: {(string.IsNullOrWhiteSpace(SubjectAlternativeNames) ? "none" : SubjectAlternativeNames)}",
                $"Key: {keySummary}",
                $"Validity: {ValidityDays} day(s)",
                $"Extensions: {(IsCertificateAuthority ? "CA" : "Leaf")} | KU: {KeyUsages}",
                $"State: {(IsEnabled ? "Enabled" : "Disabled")} | {(IsFavorite ? "Favorite" : "Standard")}"
            ]);
    }

    private static string BuildValidationSummary(TemplateValidationSummary validation)
    {
        if (validation.Errors.Count == 0 && validation.Warnings.Count == 0)
        {
            return "No validation issues.";
        }

        var lines = new List<string>();
        lines.AddRange(validation.Errors.Select(x => $"Error: {x}"));
        lines.AddRange(validation.Warnings.Select(x => $"Warning: {x}"));
        return string.Join(Environment.NewLine, lines);
    }

    private static IReadOnlyList<string> SplitValues(string value)
        => string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split([',', ';', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}
