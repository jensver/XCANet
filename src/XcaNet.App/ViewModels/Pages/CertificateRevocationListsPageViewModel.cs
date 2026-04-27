using System.Collections.ObjectModel;
using System.Windows.Input;
using XcaNet.App.Commands;
using XcaNet.Contracts.Browser;

namespace XcaNet.App.ViewModels.Pages;

public sealed class CertificateRevocationListsPageViewModel : SelectableItemsPageViewModelBase<CertificateRevocationListItem, Guid>
{
    private CertificateRevocationListInspectorData? _inspector;
    private bool _isDetailDialogOpen;
    private bool _isDeleteConfirmDialogOpen;
    private bool _isGenerateCrlDialogOpen;
    private CertificateListItem? _crlGenerateSelectedCa;
    private int _crlGenerateNextUpdateDays = 7;
    private DateTime? _crlGenerateNextUpdateDate = DateTime.Today.AddDays(7);

    public CertificateRevocationListsPageViewModel()
        : base("Revocation lists")
    {
        EmptyStateTitle = "No CRLs available";
        EmptyStateMessage = "Generate a CRL from a CA certificate after revoking one or more issued certificates.";

        OpenDeleteConfirmCommand = new DelegateCommand(() => { if (SelectedItem is not null) IsDeleteConfirmDialogOpen = true; });
        CloseDeleteConfirmCommand = new DelegateCommand(() => IsDeleteConfirmDialogOpen = false);
        CloseDetailCommand = new DelegateCommand(() => IsDetailDialogOpen = false);
        CloseGenerateCrlDialogCommand = new DelegateCommand(() => IsGenerateCrlDialogOpen = false);
    }

    public string PlaceholderMessage => "Generate CRLs from the certificates page for CA certificates with an available issuer key.";

    public ObservableCollection<CertificateListItem> CaItems { get; } = [];

    public CertificateRevocationListInspectorData? Inspector
    {
        get => _inspector;
        set => SetProperty(ref _inspector, value);
    }

    public bool IsDetailDialogOpen
    {
        get => _isDetailDialogOpen;
        set => SetProperty(ref _isDetailDialogOpen, value);
    }

    public bool IsDeleteConfirmDialogOpen
    {
        get => _isDeleteConfirmDialogOpen;
        set => SetProperty(ref _isDeleteConfirmDialogOpen, value);
    }

    public bool IsGenerateCrlDialogOpen
    {
        get => _isGenerateCrlDialogOpen;
        set => SetProperty(ref _isGenerateCrlDialogOpen, value);
    }

    public CertificateListItem? CrlGenerateSelectedCa
    {
        get => _crlGenerateSelectedCa;
        set => SetProperty(ref _crlGenerateSelectedCa, value);
    }

    public int CrlGenerateNextUpdateDays
    {
        get => _crlGenerateNextUpdateDays;
        set
        {
            if (SetProperty(ref _crlGenerateNextUpdateDays, value))
            {
                _crlGenerateNextUpdateDate = DateTime.Today.AddDays(value);
                OnPropertyChanged(nameof(CrlGenerateNextUpdateDate));
            }
        }
    }

    public DateTime? CrlGenerateNextUpdateDate
    {
        get => _crlGenerateNextUpdateDate;
        set
        {
            if (SetProperty(ref _crlGenerateNextUpdateDate, value))
            {
                if (value is { } d)
                {
                    _crlGenerateNextUpdateDays = Math.Max(1, (int)Math.Round((d - DateTime.Today).TotalDays, MidpointRounding.AwayFromZero));
                    OnPropertyChanged(nameof(CrlGenerateNextUpdateDays));
                }
            }
        }
    }

    // --- Commands ---

    public ICommand? ExportSelectedCommand { get; set; }

    public ICommand? ImportCommand { get; set; }

    public ICommand? OpenIssuerCommand { get; set; }

    public ICommand? ShowDetailsCommand { get; set; }

    public DelegateCommand CloseDetailCommand { get; }

    public ICommand? DeleteSelectedCommand { get; set; }

    public DelegateCommand OpenDeleteConfirmCommand { get; }

    public DelegateCommand CloseDeleteConfirmCommand { get; }

    public ICommand? OpenGenerateCrlDialogCommand { get; set; }

    public DelegateCommand CloseGenerateCrlDialogCommand { get; }

    public ICommand? GenerateCrlCommand { get; set; }

    protected override Guid GetItemId(CertificateRevocationListItem item) => item.CertificateRevocationListId;
}
