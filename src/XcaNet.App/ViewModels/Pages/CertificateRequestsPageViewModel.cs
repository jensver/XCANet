using System.Windows.Input;
using XcaNet.App.Commands;
using XcaNet.Contracts.Browser;

namespace XcaNet.App.ViewModels.Pages;

public sealed class CertificateRequestsPageViewModel : SelectableItemsPageViewModelBase<CertificateRequestListItem, Guid>
{
    private CryptoFormatView _selectedExportFormat = CryptoFormatView.Pem;
    private string _exportPreview = string.Empty;
    private bool _isDetailDialogOpen;

    public CertificateRequestsPageViewModel()
        : base("Certificate signing requests")
    {
        EmptyStateTitle = "No certificate signing requests";
        EmptyStateMessage = "Create a CSR from the Private Keys page or import request files to review and sign them.";

        ShowDetailsCommand = new DelegateCommand(() => { if (SelectedItem is not null) IsDetailDialogOpen = true; });
        CloseDetailCommand = new DelegateCommand(() => IsDetailDialogOpen = false);
    }

    public CertificateAuthoringViewModel IssuanceAuthoring { get; } = new(
        "Certificate Input",
        "Operation: sign selected request into a certificate",
        "Source: selected certificate request",
        "Issued Certificate",
        "CN=issued.example.test",
        365,
        false,
        "DigitalSignature, KeyEncipherment",
        "Server Authentication",
        true,
        false,
        true,
        true,
        true,
        "Sign CSR");

    public IReadOnlyList<CertificateListItem> IssuerCertificates => IssuanceAuthoring.IssuerCertificates;

    public IReadOnlyList<PrivateKeyListItem> IssuerPrivateKeys => IssuanceAuthoring.IssuerPrivateKeys;

    public IReadOnlyList<CryptoFormatView> ExportFormats { get; } = [CryptoFormatView.Pem, CryptoFormatView.Der, CryptoFormatView.Pkcs10];

    public DelegateCommand ShowDetailsCommand { get; }

    public DelegateCommand CloseDetailCommand { get; }

    public bool IsDetailDialogOpen
    {
        get => _isDetailDialogOpen;
        set => SetProperty(ref _isDetailDialogOpen, value);
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

    public ICommand? SignSelectedCommand { get; set; }

    public ICommand? OpenIssuanceAuthoringCommand { get; set; }

    public ICommand? ExportSelectedCommand { get; set; }

    public ICommand? ExportSelectedToFileCommand { get; set; }

    public ICommand? OpenSelectedPrivateKeyCommand { get; set; }

    public ICommand? ApplyIssuanceTemplateCommand { get; set; }

    public ICommand? CreateTemplateFromRequestCommand { get; set; }

    public ICommand? CreateSimilarRequestCommand { get; set; }

    public ICommand? ImportCommand { get; set; }

    public ICommand? DeleteSelectedCommand { get; set; }

    public ICommand? OpenNewRequestAuthoringCommand { get; set; }

    public ICommand? OpenRenameDialogCommand { get; set; }

    public void SetIssuers(IEnumerable<CertificateListItem> certificates, IEnumerable<PrivateKeyListItem> privateKeys)
    {
        IssuanceAuthoring.SetIssuers(certificates, privateKeys);
    }

    public void SetTemplates(IEnumerable<TemplateListItem> templates)
    {
        IssuanceAuthoring.SetTemplates(templates.Where(x => x.IsEnabled && (
            x.IntendedUsage == TemplateIntendedUsage.IntermediateCa
            || x.IntendedUsage == TemplateIntendedUsage.EndEntityCertificate)));
    }

    protected override Guid GetItemId(CertificateRequestListItem item) => item.CertificateSigningRequestId;
}
