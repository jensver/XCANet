using System.Collections.ObjectModel;
using System.Windows.Input;
using XcaNet.Contracts.Browser;

namespace XcaNet.App.ViewModels.Pages;

public sealed class CertificateRequestsPageViewModel : PageViewModelBase
{
    private CertificateRequestListItem? _selectedItem;
    private CertificateListItem? _selectedIssuerCertificate;
    private PrivateKeyListItem? _selectedIssuerPrivateKey;
    private string _issuedCertificateDisplayName = "Issued Certificate";
    private int _validityDays = 365;
    private CryptoFormatView _selectedExportFormat = CryptoFormatView.Pem;
    private string _exportPreview = string.Empty;

    public CertificateRequestsPageViewModel()
        : base("CSRs")
    {
    }

    public ObservableCollection<CertificateRequestListItem> Items { get; } = [];

    public ObservableCollection<CertificateListItem> IssuerCertificates { get; } = [];

    public ObservableCollection<PrivateKeyListItem> IssuerPrivateKeys { get; } = [];

    public IReadOnlyList<CryptoFormatView> ExportFormats { get; } = [CryptoFormatView.Pem, CryptoFormatView.Der, CryptoFormatView.Pkcs10];

    public CertificateRequestListItem? SelectedItem
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

    public string IssuedCertificateDisplayName
    {
        get => _issuedCertificateDisplayName;
        set => SetProperty(ref _issuedCertificateDisplayName, value);
    }

    public int ValidityDays
    {
        get => _validityDays;
        set => SetProperty(ref _validityDays, value);
    }

    public CryptoFormatView SelectedExportFormat
    {
        get => _selectedExportFormat;
        set => SetProperty(ref _selectedExportFormat, value);
    }

    public string ExportPreview
    {
        get => _exportPreview;
        set => SetProperty(ref _exportPreview, value);
    }

    public bool HasSelection => SelectedItem is not null;

    public ICommand? RefreshCommand { get; set; }

    public ICommand? SignSelectedCommand { get; set; }

    public ICommand? ExportSelectedCommand { get; set; }

    public ICommand? OpenSelectedPrivateKeyCommand { get; set; }

    public void SetItems(IEnumerable<CertificateRequestListItem> items)
    {
        var previousSelectionId = SelectedItem?.CertificateSigningRequestId;
        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(item);
        }

        SelectedItem = previousSelectionId.HasValue
            ? Items.FirstOrDefault(x => x.CertificateSigningRequestId == previousSelectionId.Value) ?? Items.FirstOrDefault()
            : Items.FirstOrDefault();
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

        SelectedIssuerCertificate ??= IssuerCertificates.FirstOrDefault();
        SelectedIssuerPrivateKey ??= IssuerPrivateKeys.FirstOrDefault();
    }
}
