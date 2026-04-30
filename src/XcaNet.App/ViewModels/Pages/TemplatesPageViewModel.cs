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
    private string _validationSummary = string.Empty;
    private string _previewSummary = "Template preview will appear here.";

    public TemplatesPageViewModel()
        : base("Templates")
    {
        EmptyStateTitle = "No templates saved";
        EmptyStateMessage = "Create a template to speed up CA, CSR, and issuance workflows.";
        Authoring.PropertyChanged += (_, _) => UpdatePreview();
        UpdateUsageDefaults();
        UpdatePreview();
    }

    public CertificateAuthoringViewModel Authoring { get; } = new(
        "Certificate Input",
        "Operation: edit or derive template defaults",
        "Source: template editor",
        "Template",
        string.Empty,
        365,
        false,
        "DigitalSignature, KeyEncipherment",
        "Server Authentication",
        false,
        true,
        false,
        true,
        true,
        "Save Template");

    public IReadOnlyList<TemplateIntendedUsage> IntendedUsages { get; } =
    [
        TemplateIntendedUsage.SelfSignedCa,
        TemplateIntendedUsage.IntermediateCa,
        TemplateIntendedUsage.EndEntityCertificate,
        TemplateIntendedUsage.CertificateSigningRequest
    ];

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

    public string PreviewSubjectSummary => GetPreviewLine(1, "Subject: not preset");

    public string PreviewExtensionSummary => GetPreviewLine(5, "Extensions: not preset");

    public string PreviewStateSummary => GetPreviewLine(6, "State: standard");

    public bool IsEditingExisting => SelectedItem is not null;

    public ICommand? CreateNewCommand { get; set; }

    public ICommand? EditTemplateCommand { get; set; }

    public ICommand? SaveTemplateCommand { get; set; }

    public ICommand? CloneTemplateCommand { get; set; }

    public ICommand? ToggleFavoriteCommand { get; set; }

    public ICommand? ToggleEnabledCommand { get; set; }

    public ICommand? DeleteTemplateCommand { get; set; }

    public ICommand? OpenRenameDialogCommand { get; set; }

    public ICommand? OpenObjectPropertiesCommand { get; set; }

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
        Authoring.LoadFromTemplate(template);
        Authoring.SourceSummary = $"Source: saved template {template.Name}";
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
        RaisePreviewPropertiesChanged();
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
        ValidationSummary = string.Empty;
        Authoring.ResetForNewTemplateSource("Source: new template definition");
        UpdateUsageDefaults();
        UpdatePreview();
        OnPropertyChanged(nameof(IsEditingExisting));
    }

    public void PrepareTemplateFromCertificate(CertificateListItem certificate, CertificateInspectorData? inspector)
    {
        PrepareNewTemplate();
        Name = $"{certificate.DisplayName} derived template";
        IntendedUsage = DetermineTemplateUsage(certificate, inspector);
        Authoring.LoadFromCertificate(certificate, inspector);
        UpdateUsageDefaults();
        UpdatePreview();
    }

    public void PrepareTemplateFromCertificateRequest(CertificateRequestListItem request)
    {
        PrepareNewTemplate();
        Name = $"{request.DisplayName} derived template";
        IntendedUsage = TemplateIntendedUsage.CertificateSigningRequest;
        Authoring.LoadFromCertificateRequest(request);
        UpdateUsageDefaults();
        UpdatePreview();
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
            Authoring.SubjectName,
            SplitValues(Authoring.SubjectAlternativeNames),
            Authoring.KeyAlgorithm,
            Authoring.KeyAlgorithm == KeyAlgorithmKind.Rsa ? Authoring.RsaKeySize : null,
            Authoring.KeyAlgorithm == KeyAlgorithmKind.Ecdsa ? Authoring.Curve : null,
            Authoring.SignatureAlgorithm,
            Authoring.ValidityDays,
            Authoring.IsCertificateAuthority,
            Authoring.HasPathLengthConstraint ? Authoring.PathLengthConstraint : null,
            SplitValues(Authoring.KeyUsages),
            SplitValues(Authoring.EnhancedKeyUsages));
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
            Authoring.IsCertificateAuthority = true;
            Authoring.KeyUsages = "KeyCertSign, CrlSign, DigitalSignature";
            Authoring.EnhancedKeyUsages = string.Empty;
            Authoring.ValidityDays = Math.Max(Authoring.ValidityDays, 3650);
        }
        else
        {
            Authoring.IsCertificateAuthority = false;
            Authoring.KeyUsages = string.IsNullOrWhiteSpace(Authoring.KeyUsages) || Authoring.KeyUsages.Contains("KeyCertSign", StringComparison.OrdinalIgnoreCase)
                ? "DigitalSignature, KeyEncipherment"
                : Authoring.KeyUsages;
            Authoring.EnhancedKeyUsages = string.IsNullOrWhiteSpace(Authoring.EnhancedKeyUsages) ? "Server Authentication" : Authoring.EnhancedKeyUsages;
            Authoring.ValidityDays = Math.Max(Authoring.ValidityDays, 365);
        }
    }

    private void UpdatePreview()
    {
        var keySummary = Authoring.KeyAlgorithm == KeyAlgorithmKind.Rsa ? $"RSA {Authoring.RsaKeySize}" : $"ECDSA {Authoring.Curve}";
        PreviewSummary = string.Join(
            Environment.NewLine,
            [
                $"Usage: {IntendedUsage}",
                $"Subject: {(string.IsNullOrWhiteSpace(Authoring.SubjectName) ? "not preset" : Authoring.SubjectName)}",
                $"SAN: {(string.IsNullOrWhiteSpace(Authoring.SubjectAlternativeNames) ? "none" : Authoring.SubjectAlternativeNames)}",
                $"Key: {keySummary}",
                $"Validity: {Authoring.ValidityDays} day(s)",
                $"Extensions: {(Authoring.IsCertificateAuthority ? "CA" : "Leaf")} | KU: {Authoring.KeyUsages}",
                $"State: {(IsEnabled ? "Enabled" : "Disabled")} | {(IsFavorite ? "Favorite" : "Standard")}"
            ]);
        RaisePreviewPropertiesChanged();
    }

    private static TemplateIntendedUsage DetermineTemplateUsage(CertificateListItem certificate, CertificateInspectorData? inspector)
    {
        if (certificate.IsCertificateAuthority)
        {
            return inspector is not null && string.Equals(inspector.Raw.Subject, inspector.Raw.Issuer, StringComparison.OrdinalIgnoreCase)
                ? TemplateIntendedUsage.SelfSignedCa
                : TemplateIntendedUsage.IntermediateCa;
        }

        return TemplateIntendedUsage.EndEntityCertificate;
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

    private string GetPreviewLine(int index, string fallback)
    {
        var lines = PreviewSummary.Split(Environment.NewLine, StringSplitOptions.None);
        return index < lines.Length ? lines[index] : fallback;
    }

    private void RaisePreviewPropertiesChanged()
    {
        OnPropertyChanged(nameof(PreviewSubjectSummary));
        OnPropertyChanged(nameof(PreviewExtensionSummary));
        OnPropertyChanged(nameof(PreviewStateSummary));
    }
}
