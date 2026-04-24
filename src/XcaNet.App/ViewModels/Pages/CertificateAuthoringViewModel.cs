using System.Collections.ObjectModel;
using System.Windows.Input;
using XcaNet.Contracts.Browser;
using XcaNet.Contracts.Crypto;

namespace XcaNet.App.ViewModels.Pages;

public sealed class CertificateAuthoringViewModel : ViewModelBase
{
    private string _title;
    private string _operationModeSummary;
    private string _sourceSummary;
    private string _displayName;
    private string _subjectName;
    private string _subjectAlternativeNames;
    private KeyAlgorithmKind _keyAlgorithm;
    private int _rsaKeySize;
    private EllipticCurveKind _curve;
    private string _signatureAlgorithm;
    private int _validityDays;
    private bool _isCertificateAuthority;
    private bool _hasPathLengthConstraint;
    private int _pathLengthConstraint;
    private string _keyUsages;
    private string _enhancedKeyUsages;
    private bool _showTemplateApplication;
    private bool _showKeySection;
    private bool _showSigningSection;
    private bool _showValiditySection;
    private bool _showSignatureAlgorithm;
    private string _primaryActionLabel;
    private TemplateListItem? _selectedTemplate;
    private TemplateApplicationModeView _selectedTemplateApplicationMode;
    private CertificateListItem? _selectedIssuerCertificate;
    private PrivateKeyListItem? _selectedIssuerPrivateKey;
    private ICommand? _applyTemplateCommand;
    private ICommand? _primaryActionCommand;

    public CertificateAuthoringViewModel(
        string title,
        string operationModeSummary,
        string sourceSummary,
        string displayName,
        string subjectName,
        int validityDays,
        bool isCertificateAuthority,
        string keyUsages,
        string enhancedKeyUsages,
        bool showTemplateApplication,
        bool showKeySection,
        bool showSigningSection,
        bool showValiditySection,
        bool showSignatureAlgorithm,
        string primaryActionLabel)
    {
        _title = title;
        _operationModeSummary = operationModeSummary;
        _sourceSummary = sourceSummary;
        _displayName = displayName;
        _subjectName = subjectName;
        _validityDays = validityDays;
        _isCertificateAuthority = isCertificateAuthority;
        _keyUsages = keyUsages;
        _enhancedKeyUsages = enhancedKeyUsages;
        _showTemplateApplication = showTemplateApplication;
        _showKeySection = showKeySection;
        _showSigningSection = showSigningSection;
        _showValiditySection = showValiditySection;
        _showSignatureAlgorithm = showSignatureAlgorithm;
        _primaryActionLabel = primaryActionLabel;
        _subjectAlternativeNames = string.Empty;
        _keyAlgorithm = KeyAlgorithmKind.Rsa;
        _rsaKeySize = 3072;
        _curve = EllipticCurveKind.P256;
        _signatureAlgorithm = "SHA-256";
        _selectedTemplateApplicationMode = TemplateApplicationModeView.Full;
    }

    public IReadOnlyList<KeyAlgorithmKind> KeyAlgorithms { get; } = [KeyAlgorithmKind.Rsa, KeyAlgorithmKind.Ecdsa];

    public IReadOnlyList<EllipticCurveKind> Curves { get; } = [EllipticCurveKind.P256, EllipticCurveKind.P384];

    public IReadOnlyList<TemplateApplicationModeView> TemplateApplicationModes { get; } =
        [TemplateApplicationModeView.Full, TemplateApplicationModeView.SubjectOnly, TemplateApplicationModeView.ExtensionsOnly];

    public ObservableCollection<TemplateListItem> Templates { get; } = [];

    public ObservableCollection<CertificateListItem> IssuerCertificates { get; } = [];

    public ObservableCollection<PrivateKeyListItem> IssuerPrivateKeys { get; } = [];

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string OperationModeSummary
    {
        get => _operationModeSummary;
        set => SetProperty(ref _operationModeSummary, value);
    }

    public string SourceSummary
    {
        get => _sourceSummary;
        set => SetProperty(ref _sourceSummary, value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    public string SubjectName
    {
        get => _subjectName;
        set => SetProperty(ref _subjectName, value);
    }

    public string SubjectAlternativeNames
    {
        get => _subjectAlternativeNames;
        set => SetProperty(ref _subjectAlternativeNames, value);
    }

    public KeyAlgorithmKind KeyAlgorithm
    {
        get => _keyAlgorithm;
        set
        {
            if (SetProperty(ref _keyAlgorithm, value))
            {
                OnPropertyChanged(nameof(IsRsa));
                OnPropertyChanged(nameof(IsEcdsa));
            }
        }
    }

    public bool IsRsa => KeyAlgorithm == KeyAlgorithmKind.Rsa;

    public bool IsEcdsa => KeyAlgorithm == KeyAlgorithmKind.Ecdsa;

    public int RsaKeySize
    {
        get => _rsaKeySize;
        set => SetProperty(ref _rsaKeySize, value);
    }

    public EllipticCurveKind Curve
    {
        get => _curve;
        set => SetProperty(ref _curve, value);
    }

    public string SignatureAlgorithm
    {
        get => _signatureAlgorithm;
        set => SetProperty(ref _signatureAlgorithm, value);
    }

    public int ValidityDays
    {
        get => _validityDays;
        set => SetProperty(ref _validityDays, value);
    }

    public bool IsCertificateAuthority
    {
        get => _isCertificateAuthority;
        set => SetProperty(ref _isCertificateAuthority, value);
    }

    public bool HasPathLengthConstraint
    {
        get => _hasPathLengthConstraint;
        set => SetProperty(ref _hasPathLengthConstraint, value);
    }

    public int PathLengthConstraint
    {
        get => _pathLengthConstraint;
        set => SetProperty(ref _pathLengthConstraint, value);
    }

    public string KeyUsages
    {
        get => _keyUsages;
        set => SetProperty(ref _keyUsages, value);
    }

    public string EnhancedKeyUsages
    {
        get => _enhancedKeyUsages;
        set => SetProperty(ref _enhancedKeyUsages, value);
    }

    public bool ShowTemplateApplication
    {
        get => _showTemplateApplication;
        set => SetProperty(ref _showTemplateApplication, value);
    }

    public bool ShowKeySection
    {
        get => _showKeySection;
        set => SetProperty(ref _showKeySection, value);
    }

    public bool ShowSigningSection
    {
        get => _showSigningSection;
        set => SetProperty(ref _showSigningSection, value);
    }

    public bool ShowValiditySection
    {
        get => _showValiditySection;
        set => SetProperty(ref _showValiditySection, value);
    }

    public bool ShowSignatureAlgorithm
    {
        get => _showSignatureAlgorithm;
        set => SetProperty(ref _showSignatureAlgorithm, value);
    }

    public string PrimaryActionLabel
    {
        get => _primaryActionLabel;
        set => SetProperty(ref _primaryActionLabel, value);
    }

    public TemplateListItem? SelectedTemplate
    {
        get => _selectedTemplate;
        set => SetProperty(ref _selectedTemplate, value);
    }

    public TemplateApplicationModeView SelectedTemplateApplicationMode
    {
        get => _selectedTemplateApplicationMode;
        set => SetProperty(ref _selectedTemplateApplicationMode, value);
    }

    public CertificateListItem? SelectedIssuerCertificate
    {
        get => _selectedIssuerCertificate;
        set => SetProperty(ref _selectedIssuerCertificate, value);
    }

    public PrivateKeyListItem? SelectedIssuerPrivateKey
    {
        get => _selectedIssuerPrivateKey;
        set => SetProperty(ref _selectedIssuerPrivateKey, value);
    }

    public ICommand? ApplyTemplateCommand
    {
        get => _applyTemplateCommand;
        set => SetProperty(ref _applyTemplateCommand, value);
    }

    public ICommand? PrimaryActionCommand
    {
        get => _primaryActionCommand;
        set => SetProperty(ref _primaryActionCommand, value);
    }

    public void SetTemplates(IEnumerable<TemplateListItem> templates)
    {
        Templates.Clear();
        foreach (var template in templates)
        {
            Templates.Add(template);
        }

        SelectedTemplate = Templates.FirstOrDefault(x => SelectedTemplate is not null && x.TemplateId == SelectedTemplate.TemplateId)
            ?? Templates.FirstOrDefault();
    }

    public void SetIssuers(IEnumerable<CertificateListItem> certificates, IEnumerable<PrivateKeyListItem> privateKeys)
    {
        IssuerCertificates.Clear();
        foreach (var certificate in certificates.Where(x => x.IsCertificateAuthority))
        {
            IssuerCertificates.Add(certificate);
        }

        IssuerPrivateKeys.Clear();
        foreach (var privateKey in privateKeys)
        {
            IssuerPrivateKeys.Add(privateKey);
        }

        SelectedIssuerCertificate = IssuerCertificates.FirstOrDefault(x => SelectedIssuerCertificate is not null && x.CertificateId == SelectedIssuerCertificate.CertificateId)
            ?? IssuerCertificates.FirstOrDefault();
        SelectedIssuerPrivateKey = IssuerPrivateKeys.FirstOrDefault(x => SelectedIssuerPrivateKey is not null && x.PrivateKeyId == SelectedIssuerPrivateKey.PrivateKeyId)
            ?? IssuerPrivateKeys.FirstOrDefault();
    }

    public void ApplyTemplateDefaults(AppliedTemplateDefaults defaults)
    {
        switch (SelectedTemplateApplicationMode)
        {
            case TemplateApplicationModeView.Full:
                DisplayName = defaults.DisplayNameDefault;
                SubjectName = defaults.SubjectDefault ?? SubjectName;
                SubjectAlternativeNames = string.Join(", ", defaults.SubjectAlternativeNames);
                KeyAlgorithm = defaults.KeyAlgorithm;
                RsaKeySize = defaults.RsaKeySize ?? RsaKeySize;
                Curve = defaults.Curve ?? Curve;
                SignatureAlgorithm = defaults.SignatureAlgorithm;
                ValidityDays = Math.Max(1, defaults.ValidityDays);
                IsCertificateAuthority = defaults.IsCertificateAuthority;
                HasPathLengthConstraint = defaults.PathLengthConstraint.HasValue;
                PathLengthConstraint = defaults.PathLengthConstraint ?? 0;
                KeyUsages = string.Join(", ", defaults.KeyUsages);
                EnhancedKeyUsages = string.Join(", ", defaults.EnhancedKeyUsages);
                break;
            case TemplateApplicationModeView.SubjectOnly:
                SubjectName = defaults.SubjectDefault ?? SubjectName;
                SubjectAlternativeNames = string.Join(", ", defaults.SubjectAlternativeNames);
                break;
            case TemplateApplicationModeView.ExtensionsOnly:
                IsCertificateAuthority = defaults.IsCertificateAuthority;
                HasPathLengthConstraint = defaults.PathLengthConstraint.HasValue;
                PathLengthConstraint = defaults.PathLengthConstraint ?? 0;
                KeyUsages = string.Join(", ", defaults.KeyUsages);
                EnhancedKeyUsages = string.Join(", ", defaults.EnhancedKeyUsages);
                break;
        }
    }

    public void LoadFromTemplate(TemplateDetails template)
    {
        SubjectName = template.SubjectDefault ?? string.Empty;
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
    }

    public void LoadFromCertificate(CertificateListItem certificate, CertificateInspectorData? inspector)
    {
        DisplayName = $"{certificate.DisplayName} Template";
        SubjectName = inspector?.Raw.Subject ?? certificate.Subject;
        SubjectAlternativeNames = string.Join(", ", inspector?.Extensions.SubjectAlternativeNames ?? []);
        KeyAlgorithm = ParseKeyAlgorithm(certificate.KeyAlgorithm);
        SignatureAlgorithm = "SHA-256";
        ValidityDays = BuildValidityDays(certificate.NotBefore, certificate.NotAfter, ValidityDays);
        IsCertificateAuthority = certificate.IsCertificateAuthority;
        HasPathLengthConstraint = false;
        PathLengthConstraint = 0;
        KeyUsages = string.Join(", ", inspector?.Extensions.KeyUsages ?? []);
        EnhancedKeyUsages = string.Join(", ", inspector?.Extensions.EnhancedKeyUsages ?? []);
        SourceSummary = $"Source: certificate {certificate.DisplayName}";
    }

    public void LoadFromCertificateRequest(CertificateRequestListItem request)
    {
        DisplayName = $"{request.DisplayName} Template";
        SubjectName = request.Subject;
        SubjectAlternativeNames = request.SubjectAlternativeNames;
        KeyAlgorithm = ParseKeyAlgorithm(request.KeyAlgorithm);
        SourceSummary = $"Source: request {request.DisplayName}";
    }

    public void ResetForNewTemplateSource(string sourceSummary)
    {
        SourceSummary = sourceSummary;
        DisplayName = "Template";
        SubjectName = string.Empty;
        SubjectAlternativeNames = string.Empty;
        KeyAlgorithm = KeyAlgorithmKind.Rsa;
        RsaKeySize = 3072;
        Curve = EllipticCurveKind.P256;
        SignatureAlgorithm = "SHA-256";
        ValidityDays = 365;
        IsCertificateAuthority = false;
        HasPathLengthConstraint = false;
        PathLengthConstraint = 0;
        KeyUsages = "DigitalSignature, KeyEncipherment";
        EnhancedKeyUsages = "Server Authentication";
    }

    private static int BuildValidityDays(DateTimeOffset? notBefore, DateTimeOffset? notAfter, int fallback)
    {
        if (notBefore is null || notAfter is null)
        {
            return fallback;
        }

        return Math.Max(1, (int)Math.Round((notAfter.Value - notBefore.Value).TotalDays, MidpointRounding.AwayFromZero));
    }

    private static KeyAlgorithmKind ParseKeyAlgorithm(string keyAlgorithm)
        => keyAlgorithm.Contains("ECDSA", StringComparison.OrdinalIgnoreCase)
            ? KeyAlgorithmKind.Ecdsa
            : KeyAlgorithmKind.Rsa;
}
