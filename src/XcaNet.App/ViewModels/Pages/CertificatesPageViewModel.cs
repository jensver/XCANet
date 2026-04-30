using System.Collections.ObjectModel;
using System.Windows.Input;
using XcaNet.Contracts.Browser;
using XcaNet.Contracts.Revocation;

namespace XcaNet.App.ViewModels.Pages;

public sealed class CertificatesPageViewModel : SelectableItemsPageViewModelBase<CertificateListItem, Guid>
{
    private CertificateFilterState _filter = new(null, null, null, null, null, CertificateValidityFilter.All, CertificateAuthorityFilter.All, 30);
    private CertificateInspectorData? _inspector;
    private string _importPassword = string.Empty;
    private string _importDisplayName = "Imported Material";
    private string _importPayload = string.Empty;
    private CryptoImportKindView _selectedImportKind = CryptoImportKindView.Certificate;
    private CryptoFormatView _selectedImportFormat = CryptoFormatView.Pem;
    private CryptoFormatView _selectedExportFormat = CryptoFormatView.Pem;
    private CertificateExportTargetView _selectedExportTarget = CertificateExportTargetView.Certificate;
    private string _selectedExportPassword = string.Empty;
    private string _exportPreview = string.Empty;
    private CertificateRevocationReason _selectedRevocationReason = CertificateRevocationReason.Unspecified;
    private DateTimeOffset _selectedRevocationDate = DateTimeOffset.UtcNow;
    private RelatedNavigationItem? _selectedChildNavigationItem;
    private CertificateTreeNodeViewModel? _selectedTreeNode;
    private bool _isPlainView;
    private bool _isDetailDialogOpen;
    private bool _isRevokeDialogOpen;

    public CertificatesPageViewModel()
        : base("Certificates")
    {
        EmptyStateTitle = "No certificates yet";
        EmptyStateMessage = "Import certificate material, create a self-signed CA, or sign a CSR to populate this workspace.";
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

    public IReadOnlyList<CertificateExportTargetView> ExportTargets { get; } =
        [CertificateExportTargetView.Certificate, CertificateExportTargetView.CertificateChain, CertificateExportTargetView.CertificateWithPrivateKeyPem, CertificateExportTargetView.Pkcs12Bundle];

    public IReadOnlyList<CertificateRevocationReason> RevocationReasons { get; } =
        Enum.GetValues<CertificateRevocationReason>();

    // --- Tree ---

    public ObservableCollection<CertificateTreeNodeViewModel> CertificateTreeRoots { get; } = [];

    public CertificateTreeNodeViewModel? SelectedTreeNode
    {
        get => _selectedTreeNode;
        set
        {
            if (SetProperty(ref _selectedTreeNode, value))
            {
                // Sync to the flat SelectedItem so all commands remain source-of-truth agnostic.
                if (value is not null && !ReferenceEquals(SelectedItem, value.Item))
                    SelectedItem = value.Item;
            }
        }
    }

    public bool IsPlainView
    {
        get => _isPlainView;
        set => SetProperty(ref _isPlainView, value);
    }

    // --- Dialogs ---

    public bool IsDetailDialogOpen
    {
        get => _isDetailDialogOpen;
        set => SetProperty(ref _isDetailDialogOpen, value);
    }

    public bool IsRevokeDialogOpen
    {
        get => _isRevokeDialogOpen;
        set => SetProperty(ref _isRevokeDialogOpen, value);
    }

    // --- Existing data properties ---

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

    public CertificateExportTargetView SelectedExportTarget
    {
        get => _selectedExportTarget;
        set => SetProperty(ref _selectedExportTarget, value);
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

    public RelatedNavigationItem? SelectedChildNavigationItem
    {
        get => _selectedChildNavigationItem;
        set => SetProperty(ref _selectedChildNavigationItem, value);
    }

    // --- Commands ---

    public ICommand? ImportMaterialCommand { get; set; }

    public ICommand? ImportFilesCommand { get; set; }

    public ICommand? ExportSelectedCommand { get; set; }

    public ICommand? RevokeSelectedCommand { get; set; }

    public ICommand? OpenRevokeDialogCommand { get; set; }

    public ICommand? CloseRevokeDialogCommand { get; set; }

    public ICommand? GenerateCertificateRevocationListCommand { get; set; }

    public ICommand? OpenIssuerCommand { get; set; }

    public ICommand? OpenPrivateKeyCommand { get; set; }

    public ICommand? OpenChildCertificateCommand { get; set; }

    public ICommand? CreateTemplateFromCertificateCommand { get; set; }

    public ICommand? ShowDetailsCommand { get; set; }

    public ICommand? CloseDetailCommand { get; set; }

    public ICommand? TogglePlainViewCommand { get; set; }

    public ICommand? OpenRenameDialogCommand { get; set; }

    public ICommand? OpenObjectPropertiesCommand { get; set; }

    // --- Tree building ---

    public void RebuildTree(IReadOnlyList<CertificateListItem> items)
    {
        CertificateTreeRoots.Clear();

        // Index all items by their CertificateId.
        var nodeById = new Dictionary<Guid, CertificateTreeNodeViewModel>(items.Count);
        foreach (var item in items)
            nodeById[item.CertificateId] = new CertificateTreeNodeViewModel(item);

        // Place each node under its parent, or at root if the issuer is not in the set.
        foreach (var item in items)
        {
            var node = nodeById[item.CertificateId];
            var isSelfSigned = item.IssuerCertificateId is null
                || item.IssuerCertificateId == item.CertificateId;

            if (!isSelfSigned
                && item.IssuerCertificateId is Guid parentId
                && nodeById.TryGetValue(parentId, out var parent))
            {
                parent.Children.Add(node);
            }
            else
            {
                CertificateTreeRoots.Add(node);
            }
        }
    }

    protected override Guid GetItemId(CertificateListItem item) => item.CertificateId;
}
