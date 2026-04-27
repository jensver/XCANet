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
    private DateTime? _notBeforeDate;
    private DateTime? _notAfterDate;
    private bool _isCertificateAuthority;
    private bool _hasPathLengthConstraint;
    private int _pathLengthConstraint;
    private string _keyUsages;
    private string _enhancedKeyUsages;
    private string _comment;
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

    // Structured DN backing fields
    private string _dnCommonName = string.Empty;
    private string _dnOrganization = string.Empty;
    private string _dnOrganizationalUnit = string.Empty;
    private string _dnCountry = string.Empty;
    private string _dnState = string.Empty;
    private string _dnLocality = string.Empty;
    private string _dnEmail = string.Empty;
    private bool _syncingSubject;

    // KU backing fields
    private bool _kuDigitalSignature;
    private bool _kuNonRepudiation;
    private bool _kuKeyEncipherment;
    private bool _kuDataEncipherment;
    private bool _kuKeyAgreement;
    private bool _kuKeyCertSign;
    private bool _kuCrlSign;
    private bool _kuEncipherOnly;
    private bool _kuDecipherOnly;
    private bool _syncingKu;

    // EKU backing fields
    private bool _ekuServerAuth;
    private bool _ekuClientAuth;
    private bool _ekuCodeSigning;
    private bool _ekuEmailProtection;
    private bool _ekuTimeStamping;
    private bool _ekuOcspSigning;
    private bool _syncingEku;

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
        _comment = string.Empty;
        _keyAlgorithm = KeyAlgorithmKind.Rsa;
        _rsaKeySize = 3072;
        _curve = EllipticCurveKind.P256;
        _signatureAlgorithm = "SHA-256";
        _selectedTemplateApplicationMode = TemplateApplicationModeView.Full;

        var today = DateTime.Today;
        _notBeforeDate = today;
        _notAfterDate = today.AddDays(validityDays);

        ParseSubjectNameToDn(subjectName);
        ParseKeyUsagesToBools(keyUsages);
        ParseEnhancedKeyUsagesToBools(enhancedKeyUsages);
    }

    // ── Static option lists ────────────────────────────────────────────────

    public IReadOnlyList<KeyAlgorithmKind> KeyAlgorithms { get; } = [KeyAlgorithmKind.Rsa, KeyAlgorithmKind.Ecdsa];

    public IReadOnlyList<EllipticCurveKind> Curves { get; } = [EllipticCurveKind.P256, EllipticCurveKind.P384];

    public IReadOnlyList<string> SignatureAlgorithms { get; } = ["SHA-256", "SHA-384", "SHA-512"];

    public IReadOnlyList<TemplateApplicationModeView> TemplateApplicationModes { get; } =
        [TemplateApplicationModeView.Full, TemplateApplicationModeView.SubjectOnly, TemplateApplicationModeView.ExtensionsOnly];

    public ObservableCollection<TemplateListItem> Templates { get; } = [];
    public ObservableCollection<CertificateListItem> IssuerCertificates { get; } = [];
    public ObservableCollection<PrivateKeyListItem> IssuerPrivateKeys { get; } = [];

    // ── Metadata ──────────────────────────────────────────────────────────

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

    public string Comment
    {
        get => _comment;
        set => SetProperty(ref _comment, value);
    }

    // ── Subject / DN ──────────────────────────────────────────────────────

    public string SubjectName
    {
        get => _subjectName;
        set
        {
            if (SetProperty(ref _subjectName, value) && !_syncingSubject)
            {
                _syncingSubject = true;
                ParseSubjectNameToDn(value);
                _syncingSubject = false;
            }
        }
    }

    public string DnCommonName
    {
        get => _dnCommonName;
        set
        {
            if (SetProperty(ref _dnCommonName, value) && !_syncingSubject)
                RebuildSubjectName();
        }
    }

    public string DnOrganization
    {
        get => _dnOrganization;
        set
        {
            if (SetProperty(ref _dnOrganization, value) && !_syncingSubject)
                RebuildSubjectName();
        }
    }

    public string DnOrganizationalUnit
    {
        get => _dnOrganizationalUnit;
        set
        {
            if (SetProperty(ref _dnOrganizationalUnit, value) && !_syncingSubject)
                RebuildSubjectName();
        }
    }

    public string DnCountry
    {
        get => _dnCountry;
        set
        {
            if (SetProperty(ref _dnCountry, value) && !_syncingSubject)
                RebuildSubjectName();
        }
    }

    public string DnState
    {
        get => _dnState;
        set
        {
            if (SetProperty(ref _dnState, value) && !_syncingSubject)
                RebuildSubjectName();
        }
    }

    public string DnLocality
    {
        get => _dnLocality;
        set
        {
            if (SetProperty(ref _dnLocality, value) && !_syncingSubject)
                RebuildSubjectName();
        }
    }

    public string DnEmail
    {
        get => _dnEmail;
        set
        {
            if (SetProperty(ref _dnEmail, value) && !_syncingSubject)
                RebuildSubjectName();
        }
    }

    public string SubjectAlternativeNames
    {
        get => _subjectAlternativeNames;
        set => SetProperty(ref _subjectAlternativeNames, value);
    }

    // ── Validity / Dates ──────────────────────────────────────────────────

    public int ValidityDays
    {
        get => _validityDays;
        set
        {
            if (SetProperty(ref _validityDays, value))
            {
                _notAfterDate = _notBeforeDate?.AddDays(value);
                OnPropertyChanged(nameof(NotAfterDate));
            }
        }
    }

    public DateTime? NotBeforeDate
    {
        get => _notBeforeDate;
        set
        {
            if (SetProperty(ref _notBeforeDate, value))
            {
                _notAfterDate = value?.AddDays(_validityDays);
                OnPropertyChanged(nameof(NotAfterDate));
            }
        }
    }

    public DateTime? NotAfterDate
    {
        get => _notAfterDate;
        set
        {
            if (SetProperty(ref _notAfterDate, value))
            {
                if (_notBeforeDate is { } nb && value is { } na)
                {
                    _validityDays = Math.Max(1, (int)Math.Round((na - nb).TotalDays, MidpointRounding.AwayFromZero));
                    OnPropertyChanged(nameof(ValidityDays));
                }
            }
        }
    }

    // ── Key / Algorithm ───────────────────────────────────────────────────

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

    // ── CA / Extensions ───────────────────────────────────────────────────

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

    // ── Key Usage ─────────────────────────────────────────────────────────

    public string KeyUsages
    {
        get => _keyUsages;
        set
        {
            if (SetProperty(ref _keyUsages, value) && !_syncingKu)
            {
                _syncingKu = true;
                ParseKeyUsagesToBools(value);
                _syncingKu = false;
            }
        }
    }

    public bool KuDigitalSignature { get => _kuDigitalSignature; set { if (SetProperty(ref _kuDigitalSignature, value) && !_syncingKu) RebuildKeyUsages(); } }
    public bool KuNonRepudiation { get => _kuNonRepudiation; set { if (SetProperty(ref _kuNonRepudiation, value) && !_syncingKu) RebuildKeyUsages(); } }
    public bool KuKeyEncipherment { get => _kuKeyEncipherment; set { if (SetProperty(ref _kuKeyEncipherment, value) && !_syncingKu) RebuildKeyUsages(); } }
    public bool KuDataEncipherment { get => _kuDataEncipherment; set { if (SetProperty(ref _kuDataEncipherment, value) && !_syncingKu) RebuildKeyUsages(); } }
    public bool KuKeyAgreement { get => _kuKeyAgreement; set { if (SetProperty(ref _kuKeyAgreement, value) && !_syncingKu) RebuildKeyUsages(); } }
    public bool KuKeyCertSign { get => _kuKeyCertSign; set { if (SetProperty(ref _kuKeyCertSign, value) && !_syncingKu) RebuildKeyUsages(); } }
    public bool KuCrlSign { get => _kuCrlSign; set { if (SetProperty(ref _kuCrlSign, value) && !_syncingKu) RebuildKeyUsages(); } }
    public bool KuEncipherOnly { get => _kuEncipherOnly; set { if (SetProperty(ref _kuEncipherOnly, value) && !_syncingKu) RebuildKeyUsages(); } }
    public bool KuDecipherOnly { get => _kuDecipherOnly; set { if (SetProperty(ref _kuDecipherOnly, value) && !_syncingKu) RebuildKeyUsages(); } }

    // ── Enhanced Key Usage ────────────────────────────────────────────────

    public string EnhancedKeyUsages
    {
        get => _enhancedKeyUsages;
        set
        {
            if (SetProperty(ref _enhancedKeyUsages, value) && !_syncingEku)
            {
                _syncingEku = true;
                ParseEnhancedKeyUsagesToBools(value);
                _syncingEku = false;
            }
        }
    }

    public bool EkuServerAuth { get => _ekuServerAuth; set { if (SetProperty(ref _ekuServerAuth, value) && !_syncingEku) RebuildEnhancedKeyUsages(); } }
    public bool EkuClientAuth { get => _ekuClientAuth; set { if (SetProperty(ref _ekuClientAuth, value) && !_syncingEku) RebuildEnhancedKeyUsages(); } }
    public bool EkuCodeSigning { get => _ekuCodeSigning; set { if (SetProperty(ref _ekuCodeSigning, value) && !_syncingEku) RebuildEnhancedKeyUsages(); } }
    public bool EkuEmailProtection { get => _ekuEmailProtection; set { if (SetProperty(ref _ekuEmailProtection, value) && !_syncingEku) RebuildEnhancedKeyUsages(); } }
    public bool EkuTimeStamping { get => _ekuTimeStamping; set { if (SetProperty(ref _ekuTimeStamping, value) && !_syncingEku) RebuildEnhancedKeyUsages(); } }
    public bool EkuOcspSigning { get => _ekuOcspSigning; set { if (SetProperty(ref _ekuOcspSigning, value) && !_syncingEku) RebuildEnhancedKeyUsages(); } }

    // ── Visibility flags ──────────────────────────────────────────────────

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

    // ── Commands / Actions ────────────────────────────────────────────────

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

    // ── Public mutators ───────────────────────────────────────────────────

    public void SetTemplates(IEnumerable<TemplateListItem> templates)
    {
        Templates.Clear();
        foreach (var template in templates)
            Templates.Add(template);

        SelectedTemplate = Templates.FirstOrDefault(x => SelectedTemplate is not null && x.TemplateId == SelectedTemplate.TemplateId)
            ?? Templates.FirstOrDefault();
    }

    public void SetIssuers(IEnumerable<CertificateListItem> certificates, IEnumerable<PrivateKeyListItem> privateKeys)
    {
        IssuerCertificates.Clear();
        foreach (var certificate in certificates.Where(x => x.IsCertificateAuthority))
            IssuerCertificates.Add(certificate);

        IssuerPrivateKeys.Clear();
        foreach (var privateKey in privateKeys)
            IssuerPrivateKeys.Add(privateKey);

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
        Comment = string.Empty;
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

    // ── Private helpers ───────────────────────────────────────────────────

    private void RebuildSubjectName()
    {
        _syncingSubject = true;
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(_dnCommonName)) parts.Add($"CN={_dnCommonName}");
        if (!string.IsNullOrWhiteSpace(_dnOrganization)) parts.Add($"O={_dnOrganization}");
        if (!string.IsNullOrWhiteSpace(_dnOrganizationalUnit)) parts.Add($"OU={_dnOrganizationalUnit}");
        if (!string.IsNullOrWhiteSpace(_dnCountry)) parts.Add($"C={_dnCountry}");
        if (!string.IsNullOrWhiteSpace(_dnState)) parts.Add($"ST={_dnState}");
        if (!string.IsNullOrWhiteSpace(_dnLocality)) parts.Add($"L={_dnLocality}");
        if (!string.IsNullOrWhiteSpace(_dnEmail)) parts.Add($"E={_dnEmail}");
        _subjectName = string.Join(", ", parts);
        OnPropertyChanged(nameof(SubjectName));
        _syncingSubject = false;
    }

    private void ParseSubjectNameToDn(string subjectName)
    {
        var components = ParseDnComponents(subjectName);
        _dnCommonName = components.GetValueOrDefault("CN", string.Empty);
        _dnOrganization = components.GetValueOrDefault("O", string.Empty);
        _dnOrganizationalUnit = components.GetValueOrDefault("OU", string.Empty);
        _dnCountry = components.GetValueOrDefault("C", string.Empty);
        _dnState = components.GetValueOrDefault("ST", string.Empty);
        _dnLocality = components.GetValueOrDefault("L", string.Empty);
        _dnEmail = components.GetValueOrDefault("E", components.GetValueOrDefault("EMAIL", string.Empty));
        OnPropertyChanged(nameof(DnCommonName));
        OnPropertyChanged(nameof(DnOrganization));
        OnPropertyChanged(nameof(DnOrganizationalUnit));
        OnPropertyChanged(nameof(DnCountry));
        OnPropertyChanged(nameof(DnState));
        OnPropertyChanged(nameof(DnLocality));
        OnPropertyChanged(nameof(DnEmail));
    }

    private static Dictionary<string, string> ParseDnComponents(string dn)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(dn))
            return result;
        foreach (var part in dn.Split(','))
        {
            var eq = part.IndexOf('=');
            if (eq > 0)
                result[part[..eq].Trim()] = part[(eq + 1)..].Trim();
        }
        return result;
    }

    private void RebuildKeyUsages()
    {
        _syncingKu = true;
        var parts = new List<string>();
        if (_kuDigitalSignature) parts.Add("DigitalSignature");
        if (_kuNonRepudiation) parts.Add("NonRepudiation");
        if (_kuKeyEncipherment) parts.Add("KeyEncipherment");
        if (_kuDataEncipherment) parts.Add("DataEncipherment");
        if (_kuKeyAgreement) parts.Add("KeyAgreement");
        if (_kuKeyCertSign) parts.Add("KeyCertSign");
        if (_kuCrlSign) parts.Add("CrlSign");
        if (_kuEncipherOnly) parts.Add("EncipherOnly");
        if (_kuDecipherOnly) parts.Add("DecipherOnly");
        _keyUsages = string.Join(", ", parts);
        OnPropertyChanged(nameof(KeyUsages));
        _syncingKu = false;
    }

    private void ParseKeyUsagesToBools(string value)
    {
        var set = SplitToSet(value);
        _kuDigitalSignature = set.Contains("DigitalSignature");
        _kuNonRepudiation = set.Contains("NonRepudiation");
        _kuKeyEncipherment = set.Contains("KeyEncipherment");
        _kuDataEncipherment = set.Contains("DataEncipherment");
        _kuKeyAgreement = set.Contains("KeyAgreement");
        _kuKeyCertSign = set.Contains("KeyCertSign");
        _kuCrlSign = set.Contains("CrlSign");
        _kuEncipherOnly = set.Contains("EncipherOnly");
        _kuDecipherOnly = set.Contains("DecipherOnly");
        OnPropertyChanged(nameof(KuDigitalSignature));
        OnPropertyChanged(nameof(KuNonRepudiation));
        OnPropertyChanged(nameof(KuKeyEncipherment));
        OnPropertyChanged(nameof(KuDataEncipherment));
        OnPropertyChanged(nameof(KuKeyAgreement));
        OnPropertyChanged(nameof(KuKeyCertSign));
        OnPropertyChanged(nameof(KuCrlSign));
        OnPropertyChanged(nameof(KuEncipherOnly));
        OnPropertyChanged(nameof(KuDecipherOnly));
    }

    private void RebuildEnhancedKeyUsages()
    {
        _syncingEku = true;
        var parts = new List<string>();
        if (_ekuServerAuth) parts.Add("Server Authentication");
        if (_ekuClientAuth) parts.Add("Client Authentication");
        if (_ekuCodeSigning) parts.Add("Code Signing");
        if (_ekuEmailProtection) parts.Add("Email Protection");
        if (_ekuTimeStamping) parts.Add("Time Stamping");
        if (_ekuOcspSigning) parts.Add("OCSP Signing");
        _enhancedKeyUsages = string.Join(", ", parts);
        OnPropertyChanged(nameof(EnhancedKeyUsages));
        _syncingEku = false;
    }

    private void ParseEnhancedKeyUsagesToBools(string value)
    {
        var set = SplitToSet(value);
        _ekuServerAuth = set.Contains("Server Authentication");
        _ekuClientAuth = set.Contains("Client Authentication");
        _ekuCodeSigning = set.Contains("Code Signing");
        _ekuEmailProtection = set.Contains("Email Protection");
        _ekuTimeStamping = set.Contains("Time Stamping");
        _ekuOcspSigning = set.Contains("OCSP Signing");
        OnPropertyChanged(nameof(EkuServerAuth));
        OnPropertyChanged(nameof(EkuClientAuth));
        OnPropertyChanged(nameof(EkuCodeSigning));
        OnPropertyChanged(nameof(EkuEmailProtection));
        OnPropertyChanged(nameof(EkuTimeStamping));
        OnPropertyChanged(nameof(EkuOcspSigning));
    }

    private static HashSet<string> SplitToSet(string value)
        => value.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static int BuildValidityDays(DateTimeOffset? notBefore, DateTimeOffset? notAfter, int fallback)
    {
        if (notBefore is null || notAfter is null)
            return fallback;
        return Math.Max(1, (int)Math.Round((notAfter.Value - notBefore.Value).TotalDays, MidpointRounding.AwayFromZero));
    }

    private static KeyAlgorithmKind ParseKeyAlgorithm(string keyAlgorithm)
        => keyAlgorithm.Contains("ECDSA", StringComparison.OrdinalIgnoreCase)
            ? KeyAlgorithmKind.Ecdsa
            : KeyAlgorithmKind.Rsa;
}
