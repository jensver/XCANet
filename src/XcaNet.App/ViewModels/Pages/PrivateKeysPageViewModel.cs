using System.Collections.ObjectModel;
using System.Windows.Input;
using XcaNet.Contracts.Browser;

namespace XcaNet.App.ViewModels.Pages;

public sealed class PrivateKeysPageViewModel : PageViewModelBase
{
    private PrivateKeyListItem? _selectedItem;
    private string _newKeyDisplayName = "Managed Key";
    private KeyAlgorithmView _selectedAlgorithm = KeyAlgorithmView.Rsa;
    private EllipticCurveView _selectedCurve = EllipticCurveView.P256;
    private string _selfSignedCaDisplayName = "Self-Signed CA";
    private string _selfSignedCaSubjectName = "CN=XcaNet Root CA";
    private int _selfSignedCaValidityDays = 3650;
    private string _certificateSigningRequestDisplayName = "Certificate Signing Request";
    private string _certificateSigningRequestSubjectName = "CN=service.example.test";
    private string _certificateSigningRequestSubjectAlternativeNames = "service.example.test";
    private CryptoFormatView _selectedExportFormat = CryptoFormatView.Pem;
    private string _selectedExportPassword = string.Empty;
    private string _exportPreview = string.Empty;

    public PrivateKeysPageViewModel()
        : base("Private Keys")
    {
    }

    public ObservableCollection<PrivateKeyListItem> Items { get; } = [];

    public IReadOnlyList<KeyAlgorithmView> Algorithms { get; } = [KeyAlgorithmView.Rsa, KeyAlgorithmView.Ecdsa];

    public IReadOnlyList<EllipticCurveView> Curves { get; } = [EllipticCurveView.P256, EllipticCurveView.P384];

    public IReadOnlyList<CryptoFormatView> ExportFormats { get; } = [CryptoFormatView.Pem, CryptoFormatView.Der, CryptoFormatView.Pkcs8];

    public PrivateKeyListItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                OnPropertyChanged(nameof(HasSelection));
            }
        }
    }

    public string NewKeyDisplayName
    {
        get => _newKeyDisplayName;
        set => SetProperty(ref _newKeyDisplayName, value);
    }

    public KeyAlgorithmView SelectedAlgorithm
    {
        get => _selectedAlgorithm;
        set => SetProperty(ref _selectedAlgorithm, value);
    }

    public EllipticCurveView SelectedCurve
    {
        get => _selectedCurve;
        set => SetProperty(ref _selectedCurve, value);
    }

    public string SelfSignedCaDisplayName
    {
        get => _selfSignedCaDisplayName;
        set => SetProperty(ref _selfSignedCaDisplayName, value);
    }

    public string SelfSignedCaSubjectName
    {
        get => _selfSignedCaSubjectName;
        set => SetProperty(ref _selfSignedCaSubjectName, value);
    }

    public int SelfSignedCaValidityDays
    {
        get => _selfSignedCaValidityDays;
        set => SetProperty(ref _selfSignedCaValidityDays, value);
    }

    public string CertificateSigningRequestDisplayName
    {
        get => _certificateSigningRequestDisplayName;
        set => SetProperty(ref _certificateSigningRequestDisplayName, value);
    }

    public string CertificateSigningRequestSubjectName
    {
        get => _certificateSigningRequestSubjectName;
        set => SetProperty(ref _certificateSigningRequestSubjectName, value);
    }

    public string CertificateSigningRequestSubjectAlternativeNames
    {
        get => _certificateSigningRequestSubjectAlternativeNames;
        set => SetProperty(ref _certificateSigningRequestSubjectAlternativeNames, value);
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

    public bool HasSelection => SelectedItem is not null;

    public ICommand? RefreshCommand { get; set; }

    public ICommand? GenerateKeyCommand { get; set; }

    public ICommand? CreateSelfSignedCaCommand { get; set; }

    public ICommand? CreateCertificateSigningRequestCommand { get; set; }

    public ICommand? ExportSelectedCommand { get; set; }

    public void SetItems(IEnumerable<PrivateKeyListItem> items)
    {
        var previousSelectionId = SelectedItem?.PrivateKeyId;
        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(item);
        }

        SelectedItem = previousSelectionId.HasValue
            ? Items.FirstOrDefault(x => x.PrivateKeyId == previousSelectionId.Value) ?? Items.FirstOrDefault()
            : Items.FirstOrDefault();
    }
}
