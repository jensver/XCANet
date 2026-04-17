using System.Collections.ObjectModel;
using System.Windows.Input;
using XcaNet.App.ViewModels.Items;
using XcaNet.Contracts.Browser;

namespace XcaNet.App.ViewModels.Pages;

public sealed class CertificatesPageViewModel : PageViewModelBase
{
    private CertificateListItem? _selectedItem;
    private string _searchText = string.Empty;
    private CertificateValidityFilter _selectedValidityFilter = CertificateValidityFilter.All;
    private CertificateAuthorityFilter _selectedAuthorityFilter = CertificateAuthorityFilter.All;
    private string _importDisplayName = "Imported Material";
    private string _importPayload = string.Empty;
    private string _importPassword = string.Empty;
    private CryptoImportKindView _selectedImportKind = CryptoImportKindView.Certificate;
    private CryptoFormatView _selectedImportFormat = CryptoFormatView.Pem;
    private CryptoFormatView _selectedExportFormat = CryptoFormatView.Pem;
    private string _selectedExportPassword = string.Empty;
    private string _exportPreview = string.Empty;

    public CertificatesPageViewModel()
        : base("Certificates")
    {
    }

    public ObservableCollection<CertificateListItem> Items { get; } = [];

    public IReadOnlyList<CertificateValidityFilter> ValidityFilters { get; } =
        [CertificateValidityFilter.All, CertificateValidityFilter.Valid, CertificateValidityFilter.ExpiringSoon, CertificateValidityFilter.Expired, CertificateValidityFilter.Revoked];

    public IReadOnlyList<CertificateAuthorityFilter> AuthorityFilters { get; } =
        [CertificateAuthorityFilter.All, CertificateAuthorityFilter.Authorities, CertificateAuthorityFilter.LeafCertificates];

    public IReadOnlyList<CryptoImportKindView> ImportKinds { get; } =
        [CryptoImportKindView.Certificate, CryptoImportKindView.PrivateKey, CryptoImportKindView.CertificateSigningRequest];

    public IReadOnlyList<CryptoFormatView> ImportFormats { get; } =
        [CryptoFormatView.Pem, CryptoFormatView.Der, CryptoFormatView.Pkcs8, CryptoFormatView.Pkcs10, CryptoFormatView.Pkcs12];

    public IReadOnlyList<CryptoFormatView> ExportFormats { get; } =
        [CryptoFormatView.Pem, CryptoFormatView.Der, CryptoFormatView.Pkcs12];

    public CertificateInspectorViewModel Inspector { get; } = new();

    public CertificateListItem? SelectedItem
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

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public CertificateValidityFilter SelectedValidityFilter
    {
        get => _selectedValidityFilter;
        set => SetProperty(ref _selectedValidityFilter, value);
    }

    public CertificateAuthorityFilter SelectedAuthorityFilter
    {
        get => _selectedAuthorityFilter;
        set => SetProperty(ref _selectedAuthorityFilter, value);
    }

    public string ImportDisplayName
    {
        get => _importDisplayName;
        set => SetProperty(ref _importDisplayName, value);
    }

    public string ImportPayload
    {
        get => _importPayload;
        set => SetProperty(ref _importPayload, value);
    }

    public string ImportPassword
    {
        get => _importPassword;
        set => SetProperty(ref _importPassword, value);
    }

    public CryptoImportKindView SelectedImportKind
    {
        get => _selectedImportKind;
        set => SetProperty(ref _selectedImportKind, value);
    }

    public CryptoFormatView SelectedImportFormat
    {
        get => _selectedImportFormat;
        set => SetProperty(ref _selectedImportFormat, value);
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

    public ICommand? ImportMaterialCommand { get; set; }

    public ICommand? ExportSelectedCommand { get; set; }

    public ICommand? OpenIssuerCommand { get; set; }

    public ICommand? OpenPrivateKeyCommand { get; set; }

    public ICommand? OpenChildCertificateCommand { get; set; }

    public void SetItems(IEnumerable<CertificateListItem> items)
    {
        var previousSelectionId = SelectedItem?.CertificateId;
        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(item);
        }

        SelectedItem = previousSelectionId.HasValue
            ? Items.FirstOrDefault(x => x.CertificateId == previousSelectionId.Value) ?? Items.FirstOrDefault()
            : Items.FirstOrDefault();
    }
}
