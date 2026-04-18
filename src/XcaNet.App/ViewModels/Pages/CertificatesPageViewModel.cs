using System.Collections.ObjectModel;
using System.Windows.Input;
using XcaNet.Contracts.Browser;
using XcaNet.Contracts.Revocation;

namespace XcaNet.App.ViewModels.Pages;

public sealed class CertificatesPageViewModel : SelectableItemsPageViewModelBase<CertificateListItem, Guid>
{
    private CertificateFilterState _filter = new(null, null, null, null, null, CertificateValidityFilter.All, CertificateAuthorityFilter.All, 30);
    private CertificateInspectorData? _inspector;
    private string _importDisplayName = "Imported Material";
    private string _importPayload = string.Empty;
    private string _importPassword = string.Empty;
    private CryptoImportKindView _selectedImportKind = CryptoImportKindView.Certificate;
    private CryptoFormatView _selectedImportFormat = CryptoFormatView.Pem;
    private CryptoFormatView _selectedExportFormat = CryptoFormatView.Pem;
    private string _selectedExportPassword = string.Empty;
    private string _exportPreview = string.Empty;
    private CertificateRevocationReason _selectedRevocationReason = CertificateRevocationReason.Unspecified;
    private DateTimeOffset _selectedRevocationDate = DateTimeOffset.UtcNow;
    private string _revocationConfirmationText = string.Empty;
    private RelatedNavigationItem? _selectedChildNavigationItem;

    public CertificatesPageViewModel()
        : base("Certificates")
    {
    }

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

    public IReadOnlyList<CertificateRevocationReason> RevocationReasons { get; } =
        Enum.GetValues<CertificateRevocationReason>();

    public CertificateFilterState Filter
    {
        get => _filter;
        set => SetProperty(ref _filter, value);
    }

    public CertificateInspectorData? Inspector
    {
        get => _inspector;
        set => SetProperty(ref _inspector, value);
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

    public CertificateRevocationReason SelectedRevocationReason
    {
        get => _selectedRevocationReason;
        set => SetProperty(ref _selectedRevocationReason, value);
    }

    public DateTimeOffset SelectedRevocationDate
    {
        get => _selectedRevocationDate;
        set => SetProperty(ref _selectedRevocationDate, value);
    }

    public string RevocationConfirmationText
    {
        get => _revocationConfirmationText;
        set
        {
            if (SetProperty(ref _revocationConfirmationText, value))
            {
                OnPropertyChanged(nameof(IsRevocationConfirmed));
            }
        }
    }

    public bool IsRevocationConfirmed => string.Equals(RevocationConfirmationText, "REVOKE", StringComparison.Ordinal);

    public RelatedNavigationItem? SelectedChildNavigationItem
    {
        get => _selectedChildNavigationItem;
        set => SetProperty(ref _selectedChildNavigationItem, value);
    }

    public ICommand? ImportMaterialCommand { get; set; }

    public ICommand? ExportSelectedCommand { get; set; }

    public ICommand? RevokeSelectedCommand { get; set; }

    public ICommand? GenerateCertificateRevocationListCommand { get; set; }

    public ICommand? OpenIssuerCommand { get; set; }

    public ICommand? OpenPrivateKeyCommand { get; set; }

    public ICommand? OpenChildCertificateCommand { get; set; }

    protected override Guid GetItemId(CertificateListItem item) => item.CertificateId;
}
