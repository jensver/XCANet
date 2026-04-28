using System.Collections.ObjectModel;
using System.Windows.Input;
using XcaNet.Contracts.Browser;

namespace XcaNet.App.ViewModels.Pages;

public sealed class CertificateRevocationListsPageViewModel : SelectableItemsPageViewModelBase<CertificateRevocationListItem, Guid>
{
    private CertificateRevocationListInspectorData? _inspector;

    public CertificateRevocationListsPageViewModel()
        : base("Revocation lists")
    {
        EmptyStateTitle = "No CRLs available";
        EmptyStateMessage = "Generate a CRL from a CA certificate after revoking one or more issued certificates.";
    }

    public string PlaceholderMessage => "Generate CRLs from the certificates page for CA certificates with an available issuer key.";

    public CertificateRevocationListInspectorData? Inspector
    {
        get => _inspector;
        set => SetProperty(ref _inspector, value);
    }

    public ICommand? ExportSelectedCommand { get; set; }

    public ICommand? OpenIssuerCommand { get; set; }

    public ICommand? OpenRenameDialogCommand { get; set; }

    protected override Guid GetItemId(CertificateRevocationListItem item) => item.CertificateRevocationListId;
}
