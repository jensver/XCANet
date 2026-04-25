using System.Collections.ObjectModel;
using System.Windows.Input;
using XcaNet.App.Commands;
using XcaNet.Contracts.Browser;

namespace XcaNet.App.ViewModels.Pages;

public sealed class PrivateKeysPageViewModel : SelectableItemsPageViewModelBase<PrivateKeyListItem, Guid>
{
    private string _newKeyDisplayName = "New Key";
    private KeyAlgorithmView _selectedAlgorithm = KeyAlgorithmView.Rsa;
    private EllipticCurveView _selectedCurve = EllipticCurveView.P256;
    private int _selectedKeySize = 3072;
    private CryptoFormatView _selectedExportFormat = CryptoFormatView.Pem;
    private string _selectedExportPassword = string.Empty;
    private string _exportPreview = string.Empty;
    private bool _isNewKeyDialogOpen;
    private bool _isKeyDetailDialogOpen;
    private bool _isDeleteConfirmDialogOpen;

    public PrivateKeysPageViewModel()
        : base("Private Keys")
    {
        EmptyStateTitle = "No private keys stored";
        EmptyStateMessage = "Generate a key or import existing key material to begin issuing certificates and CSRs.";

        OpenNewKeyDialogCommand = new DelegateCommand(() => IsNewKeyDialogOpen = true);
        CloseNewKeyDialogCommand = new DelegateCommand(() => IsNewKeyDialogOpen = false);
        ShowDetailsCommand = new DelegateCommand(() => { if (SelectedItem is not null) IsKeyDetailDialogOpen = true; });
        CloseKeyDetailCommand = new DelegateCommand(() => IsKeyDetailDialogOpen = false);
        OpenDeleteConfirmCommand = new DelegateCommand(() => { if (SelectedItem is not null) IsDeleteConfirmDialogOpen = true; });
        CloseDeleteConfirmCommand = new DelegateCommand(() => IsDeleteConfirmDialogOpen = false);
    }

    public IReadOnlyList<KeyAlgorithmView> Algorithms { get; } = [KeyAlgorithmView.Rsa, KeyAlgorithmView.Ecdsa];

    public IReadOnlyList<EllipticCurveView> Curves { get; } = [EllipticCurveView.P256, EllipticCurveView.P384];

    public IReadOnlyList<int> KeySizes { get; } = [2048, 3072, 4096];

    public IReadOnlyList<CryptoFormatView> ExportFormats { get; } = [CryptoFormatView.Pem, CryptoFormatView.Der, CryptoFormatView.Pkcs8];

    public DelegateCommand OpenNewKeyDialogCommand { get; }

    public DelegateCommand CloseNewKeyDialogCommand { get; }

    public DelegateCommand ShowDetailsCommand { get; }

    public DelegateCommand CloseKeyDetailCommand { get; }

    public DelegateCommand OpenDeleteConfirmCommand { get; }

    public DelegateCommand CloseDeleteConfirmCommand { get; }

    public bool IsDeleteConfirmDialogOpen
    {
        get => _isDeleteConfirmDialogOpen;
        set => SetProperty(ref _isDeleteConfirmDialogOpen, value);
    }

    public CertificateAuthoringViewModel SelfSignedCaAuthoring { get; } = new(
        "Certificate Input",
        "Operation: create self-signed CA certificate",
        "Source: selected private key",
        "Self-Signed CA",
        "CN=XcaNet Root CA",
        3650,
        true,
        "KeyCertSign, CrlSign, DigitalSignature",
        string.Empty,
        true,
        false,
        false,
        true,
        true,
        "Create CA");

    public CertificateAuthoringViewModel CertificateSigningRequestAuthoring { get; } = new(
        "Certificate Input",
        "Operation: create certificate signing request",
        "Source: selected private key",
        "Certificate Signing Request",
        "CN=service.example.test",
        365,
        false,
        "DigitalSignature, KeyEncipherment",
        "Server Authentication",
        true,
        true,
        false,
        false,
        true,
        "Create CSR");

    public bool IsNewKeyDialogOpen
    {
        get => _isNewKeyDialogOpen;
        set => SetProperty(ref _isNewKeyDialogOpen, value);
    }

    public bool IsKeyDetailDialogOpen
    {
        get => _isKeyDetailDialogOpen;
        set => SetProperty(ref _isKeyDetailDialogOpen, value);
    }

    public string NewKeyDisplayName
    {
        get => _newKeyDisplayName;
        set => SetProperty(ref _newKeyDisplayName, value);
    }

    public KeyAlgorithmView SelectedAlgorithm
    {
        get => _selectedAlgorithm;
        set
        {
            if (SetProperty(ref _selectedAlgorithm, value))
            {
                OnPropertyChanged(nameof(IsRsaAlgorithmSelected));
                OnPropertyChanged(nameof(IsEcdsaAlgorithmSelected));
            }
        }
    }

    public bool IsRsaAlgorithmSelected => _selectedAlgorithm == KeyAlgorithmView.Rsa;

    public bool IsEcdsaAlgorithmSelected => _selectedAlgorithm == KeyAlgorithmView.Ecdsa;

    public int SelectedKeySize
    {
        get => _selectedKeySize;
        set => SetProperty(ref _selectedKeySize, value);
    }

    public EllipticCurveView SelectedCurve
    {
        get => _selectedCurve;
        set => SetProperty(ref _selectedCurve, value);
    }

    public CryptoFormatView SelectedExportFormat
    {
        get => _selectedExportFormat;
        set => SetProperty(ref _selectedExportFormat, value);
    }

    public string SelectedExportPassword
    {
        get => _selectedExportPassword;
        set => SetProperty(ref _selectedExportPassword, value);
    }

    public string ExportPreview
    {
        get => _exportPreview;
        set => SetProperty(ref _exportPreview, value);
    }

    public ICommand? GenerateKeyCommand { get; set; }

    public ICommand? CreateSelfSignedCaCommand { get; set; }

    public ICommand? CreateCertificateSigningRequestCommand { get; set; }

    public ICommand? OpenSelfSignedCaAuthoringCommand { get; set; }

    public ICommand? OpenCertificateSigningRequestAuthoringCommand { get; set; }

    public ICommand? ApplySelfSignedCaTemplateCommand { get; set; }

    public ICommand? ApplyCertificateSigningRequestTemplateCommand { get; set; }

    public ICommand? ExportSelectedCommand { get; set; }

    public ICommand? ExportSelectedToFileCommand { get; set; }

    public ICommand? ImportCommand { get; set; }

    public ICommand? DeleteSelectedCommand { get; set; }

    public void SetTemplates(IEnumerable<TemplateListItem> templates)
    {
        ResetTemplateCollection(SelfSignedCaAuthoring.Templates, templates.Where(x => x.IntendedUsage == TemplateIntendedUsage.SelfSignedCa && x.IsEnabled));
        ResetTemplateCollection(
            CertificateSigningRequestAuthoring.Templates,
            templates.Where(x => x.IsEnabled && (
                x.IntendedUsage == TemplateIntendedUsage.IntermediateCa
                || x.IntendedUsage == TemplateIntendedUsage.EndEntityCertificate
                || x.IntendedUsage == TemplateIntendedUsage.CertificateSigningRequest)));
    }

    public void LoadCertificateSigningRequestAuthoringFromRequest(CertificateRequestListItem request)
    {
        CertificateSigningRequestAuthoring.LoadFromCertificateRequest(request);
        CertificateSigningRequestAuthoring.DisplayName = $"{request.DisplayName} Copy";
        CertificateSigningRequestAuthoring.SourceSummary = $"Source: similar request {request.DisplayName}";
    }

    private static void ResetTemplateCollection(ObservableCollection<TemplateListItem> target, IEnumerable<TemplateListItem> templates)
    {
        target.Clear();
        foreach (var template in templates)
        {
            target.Add(template);
        }
    }

    protected override Guid GetItemId(PrivateKeyListItem item) => item.PrivateKeyId;
}
