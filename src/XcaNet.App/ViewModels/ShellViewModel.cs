using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.Logging;
using XcaNet.App.Commands;
using XcaNet.App.ViewModels.Navigation;
using XcaNet.App.ViewModels.Notifications;
using XcaNet.App.ViewModels.Pages;
using XcaNet.Application.Services;
using XcaNet.Contracts.Browser;
using XcaNet.Contracts.Crypto;
using XcaNet.Contracts.Crypto.Workflow;
using XcaNet.Contracts.Database;
using XcaNet.Contracts.Results;
using XcaNet.Contracts.Revocation;

namespace XcaNet.App.ViewModels;

public sealed class ShellViewModel : ViewModelBase
{
    private readonly IDatabaseSessionService _databaseSessionService;
    private readonly ILogger<ShellViewModel> _logger;

    private readonly AsyncCommand _createDatabaseCommand;
    private readonly AsyncCommand _openDatabaseCommand;
    private readonly AsyncCommand _unlockDatabaseCommand;
    private readonly AsyncCommand _lockDatabaseCommand;
    private readonly AsyncCommand _refreshWorkspaceCommand;
    private readonly AsyncCommand _refreshCertificatesCommand;
    private readonly AsyncCommand _importMaterialCommand;
    private readonly AsyncCommand _exportCertificateCommand;
    private readonly AsyncCommand _revokeCertificateCommand;
    private readonly AsyncCommand _generateCertificateRevocationListCommand;
    private readonly DelegateCommand _navigateIssuerCommand;
    private readonly DelegateCommand _navigatePrivateKeyCommand;
    private readonly DelegateCommand _navigateChildCertificateCommand;
    private readonly AsyncCommand _refreshPrivateKeysCommand;
    private readonly AsyncCommand _generateKeyCommand;
    private readonly AsyncCommand _createSelfSignedCaCommand;
    private readonly AsyncCommand _createCertificateSigningRequestCommand;
    private readonly AsyncCommand _exportPrivateKeyCommand;
    private readonly AsyncCommand _refreshCertificateRequestsCommand;
    private readonly AsyncCommand _signCertificateSigningRequestCommand;
    private readonly AsyncCommand _exportCertificateSigningRequestCommand;
    private readonly DelegateCommand _navigateRequestPrivateKeyCommand;
    private readonly AsyncCommand _refreshCertificateRevocationListsCommand;
    private readonly DelegateCommand _navigateCrlIssuerCommand;
    private readonly AsyncCommand _refreshTemplatesCommand;

    private PageViewModelBase _currentPage;
    private string _subtitle = "Core UI workflows";
    private bool _isBusy;
    private string _busyMessage = string.Empty;

    public ShellViewModel(IDatabaseSessionService databaseSessionService, ILogger<ShellViewModel> logger)
    {
        _databaseSessionService = databaseSessionService;
        _logger = logger;

        logger.LogInformation("Initializing XcaNet shell.");

        DashboardPage = new DashboardPageViewModel();
        CertificatesPage = new CertificatesPageViewModel();
        PrivateKeysPage = new PrivateKeysPageViewModel();
        CertificateRequestsPage = new CertificateRequestsPageViewModel();
        CertificateRevocationListsPage = new CertificateRevocationListsPageViewModel();
        TemplatesPage = new TemplatesPageViewModel();
        SettingsSecurityPage = new SettingsSecurityPageViewModel();

        _createDatabaseCommand = new AsyncCommand(CreateDatabaseAsync, () => !IsBusy);
        _openDatabaseCommand = new AsyncCommand(OpenDatabaseAsync, () => !IsBusy);
        _unlockDatabaseCommand = new AsyncCommand(UnlockDatabaseAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Locked);
        _lockDatabaseCommand = new AsyncCommand(LockDatabaseAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked);
        _refreshWorkspaceCommand = new AsyncCommand(RefreshAllAsync, () => !IsBusy);
        _refreshCertificatesCommand = new AsyncCommand(LoadCertificatesAsync, () => !IsBusy && Snapshot.State != DatabaseSessionState.Closed);
        _importMaterialCommand = new AsyncCommand(ImportMaterialAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked);
        _exportCertificateCommand = new AsyncCommand(ExportSelectedCertificateAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked && CertificatesPage.HasSelection);
        _revokeCertificateCommand = new AsyncCommand(RevokeSelectedCertificateAsync, CanRevokeSelectedCertificate);
        _generateCertificateRevocationListCommand = new AsyncCommand(GenerateCertificateRevocationListAsync, CanGenerateCertificateRevocationList);
        _navigateIssuerCommand = new DelegateCommand(() => NavigateTo(CertificatesPage.Inspector?.Navigation.Issuer), () => CertificatesPage.Inspector?.Navigation.Issuer is not null);
        _navigatePrivateKeyCommand = new DelegateCommand(() => NavigateTo(CertificatesPage.Inspector?.Navigation.PrivateKey), () => CertificatesPage.Inspector?.Navigation.PrivateKey is not null);
        _navigateChildCertificateCommand = new DelegateCommand(() => NavigateTo(CertificatesPage.SelectedChildNavigationItem?.Target), () => CertificatesPage.SelectedChildNavigationItem is not null);
        _refreshPrivateKeysCommand = new AsyncCommand(LoadPrivateKeysAsync, () => !IsBusy && Snapshot.State != DatabaseSessionState.Closed);
        _generateKeyCommand = new AsyncCommand(GenerateKeyAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked);
        _createSelfSignedCaCommand = new AsyncCommand(CreateSelfSignedCaAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked && PrivateKeysPage.HasSelection);
        _createCertificateSigningRequestCommand = new AsyncCommand(CreateCertificateSigningRequestAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked && PrivateKeysPage.HasSelection);
        _exportPrivateKeyCommand = new AsyncCommand(ExportSelectedPrivateKeyAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked && PrivateKeysPage.HasSelection);
        _refreshCertificateRequestsCommand = new AsyncCommand(LoadCertificateRequestsAsync, () => !IsBusy && Snapshot.State != DatabaseSessionState.Closed);
        _signCertificateSigningRequestCommand = new AsyncCommand(SignCertificateSigningRequestAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked && CertificateRequestsPage.HasSelection && CertificateRequestsPage.SelectedIssuerCertificate is not null && CertificateRequestsPage.SelectedIssuerPrivateKey is not null);
        _exportCertificateSigningRequestCommand = new AsyncCommand(ExportSelectedCertificateSigningRequestAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked && CertificateRequestsPage.HasSelection);
        _navigateRequestPrivateKeyCommand = new DelegateCommand(() => NavigateTo(CertificateRequestsPage.SelectedItem?.PrivateKeyTarget), () => CertificateRequestsPage.SelectedItem?.PrivateKeyTarget is not null);
        _refreshCertificateRevocationListsCommand = new AsyncCommand(LoadCertificateRevocationListsAsync, () => !IsBusy && Snapshot.State != DatabaseSessionState.Closed);
        _navigateCrlIssuerCommand = new DelegateCommand(() => NavigateTo(CertificateRevocationListsPage.Inspector?.IssuerTarget), () => CertificateRevocationListsPage.Inspector is not null);
        _refreshTemplatesCommand = new AsyncCommand(LoadTemplatesAsync, () => !IsBusy && Snapshot.State != DatabaseSessionState.Closed);

        SettingsSecurityPage.CreateDatabaseCommand = _createDatabaseCommand;
        SettingsSecurityPage.OpenDatabaseCommand = _openDatabaseCommand;
        SettingsSecurityPage.UnlockDatabaseCommand = _unlockDatabaseCommand;
        SettingsSecurityPage.LockDatabaseCommand = _lockDatabaseCommand;

        CertificatesPage.RefreshCommand = _refreshCertificatesCommand;
        CertificatesPage.ImportMaterialCommand = _importMaterialCommand;
        CertificatesPage.ExportSelectedCommand = _exportCertificateCommand;
        CertificatesPage.RevokeSelectedCommand = _revokeCertificateCommand;
        CertificatesPage.GenerateCertificateRevocationListCommand = _generateCertificateRevocationListCommand;
        CertificatesPage.OpenIssuerCommand = _navigateIssuerCommand;
        CertificatesPage.OpenPrivateKeyCommand = _navigatePrivateKeyCommand;
        CertificatesPage.OpenChildCertificateCommand = _navigateChildCertificateCommand;

        PrivateKeysPage.RefreshCommand = _refreshPrivateKeysCommand;
        PrivateKeysPage.GenerateKeyCommand = _generateKeyCommand;
        PrivateKeysPage.CreateSelfSignedCaCommand = _createSelfSignedCaCommand;
        PrivateKeysPage.CreateCertificateSigningRequestCommand = _createCertificateSigningRequestCommand;
        PrivateKeysPage.ExportSelectedCommand = _exportPrivateKeyCommand;

        CertificateRequestsPage.RefreshCommand = _refreshCertificateRequestsCommand;
        CertificateRequestsPage.SignSelectedCommand = _signCertificateSigningRequestCommand;
        CertificateRequestsPage.ExportSelectedCommand = _exportCertificateSigningRequestCommand;
        CertificateRequestsPage.OpenSelectedPrivateKeyCommand = _navigateRequestPrivateKeyCommand;

        CertificateRevocationListsPage.RefreshCommand = _refreshCertificateRevocationListsCommand;
        CertificateRevocationListsPage.OpenIssuerCommand = _navigateCrlIssuerCommand;
        TemplatesPage.RefreshCommand = _refreshTemplatesCommand;

        NavigationItems =
        [
            new NavigationItemViewModel("Dashboard", "Overview", new DelegateCommand(() => SelectPage(DashboardPage))),
            new NavigationItemViewModel("Certificates", "Browse", new DelegateCommand(() => SelectPage(CertificatesPage))),
            new NavigationItemViewModel("Private Keys", "Secure", new DelegateCommand(() => SelectPage(PrivateKeysPage))),
            new NavigationItemViewModel("CSRs", "Requests", new DelegateCommand(() => SelectPage(CertificateRequestsPage))),
            new NavigationItemViewModel("CRLs", "Revocation", new DelegateCommand(() => SelectPage(CertificateRevocationListsPage))),
            new NavigationItemViewModel("Templates", "Presets", new DelegateCommand(() => SelectPage(TemplatesPage))),
            new NavigationItemViewModel("Settings / Security", "Database", new DelegateCommand(() => SelectPage(SettingsSecurityPage)))
        ];

        _currentPage = DashboardPage;

        CertificatesPage.PropertyChanged += OnCertificatesPagePropertyChanged;
        PrivateKeysPage.PropertyChanged += OnPageSelectionChanged;
        CertificateRequestsPage.PropertyChanged += OnPageSelectionChanged;
        CertificateRevocationListsPage.PropertyChanged += OnPageSelectionChanged;

        SelectPage(DashboardPage);
        ApplySnapshot(Snapshot);
        _ = RefreshAllAsync();
    }

    public string Title => "XcaNet";

    public string Subtitle
    {
        get => _subtitle;
        private set => SetProperty(ref _subtitle, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RefreshCommandStates();
            }
        }
    }

    public string BusyMessage
    {
        get => _busyMessage;
        private set => SetProperty(ref _busyMessage, value);
    }

    public DashboardPageViewModel DashboardPage { get; }

    public CertificatesPageViewModel CertificatesPage { get; }

    public PrivateKeysPageViewModel PrivateKeysPage { get; }

    public CertificateRequestsPageViewModel CertificateRequestsPage { get; }

    public CertificateRevocationListsPageViewModel CertificateRevocationListsPage { get; }

    public TemplatesPageViewModel TemplatesPage { get; }

    public SettingsSecurityPageViewModel SettingsSecurityPage { get; }

    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }

    public ObservableCollection<NotificationItemViewModel> Notifications { get; } = [];

    public PageViewModelBase CurrentPage
    {
        get => _currentPage;
        private set => SetProperty(ref _currentPage, value);
    }

    public DatabaseSessionSnapshot Snapshot => _databaseSessionService.GetSnapshot();

    private async Task CreateDatabaseAsync()
    {
        await RunDatabaseActionAsync(
            "Creating database",
            () => _databaseSessionService.CreateDatabaseAsync(
                new CreateDatabaseRequest(SettingsSecurityPage.DatabasePath, SettingsSecurityPage.Password, SettingsSecurityPage.DisplayName),
                CancellationToken.None));
    }

    private async Task OpenDatabaseAsync()
    {
        await RunDatabaseActionAsync(
            "Opening database",
            () => _databaseSessionService.OpenDatabaseAsync(new OpenDatabaseRequest(SettingsSecurityPage.DatabasePath), CancellationToken.None));
    }

    private async Task UnlockDatabaseAsync()
    {
        await RunDatabaseActionAsync(
            "Unlocking database",
            () => _databaseSessionService.UnlockDatabaseAsync(new UnlockDatabaseRequest(SettingsSecurityPage.Password), CancellationToken.None));
    }

    private async Task LockDatabaseAsync()
    {
        await RunDatabaseActionAsync("Locking database", () => _databaseSessionService.LockDatabaseAsync(CancellationToken.None));
    }

    private async Task GenerateKeyAsync()
    {
        var algorithm = PrivateKeysPage.SelectedAlgorithm == KeyAlgorithmView.Rsa ? KeyAlgorithmKind.Rsa : KeyAlgorithmKind.Ecdsa;
        var curve = algorithm == KeyAlgorithmKind.Ecdsa
            ? PrivateKeysPage.SelectedCurve == EllipticCurveView.P256 ? EllipticCurveKind.P256 : EllipticCurveKind.P384
            : (EllipticCurveKind?)null;

        using var scope = BeginBusy("Generating managed private key");
        var result = await _databaseSessionService.GenerateStoredKeyAsync(
            new GenerateStoredKeyRequest(PrivateKeysPage.NewKeyDisplayName, algorithm, algorithm == KeyAlgorithmKind.Rsa ? 3072 : null, curve),
            CancellationToken.None);

        if (!result.IsSuccess || result.Value is null)
        {
            NotifyFailure(result.Message);
            return;
        }

        await RefreshAllAsync();
        NavigateTo(new NavigationTarget(BrowserEntityType.PrivateKey, result.Value.PrivateKeyId, NavigationFocusSection.Overview));
        NotifySuccess($"Generated {result.Value.Algorithm} key.");
    }

    private async Task CreateSelfSignedCaAsync()
    {
        if (PrivateKeysPage.SelectedItem is null)
        {
            NotifyFailure("Select a private key first.");
            return;
        }

        using var scope = BeginBusy("Creating self-signed CA");
        var result = await _databaseSessionService.CreateSelfSignedCaAsync(
            new CreateSelfSignedCaWorkflowRequest(
                PrivateKeysPage.SelectedItem.PrivateKeyId,
                PrivateKeysPage.SelfSignedCaDisplayName,
                PrivateKeysPage.SelfSignedCaSubjectName,
                Math.Max(1, PrivateKeysPage.SelfSignedCaValidityDays)),
            CancellationToken.None);

        if (!result.IsSuccess || result.Value is null)
        {
            NotifyFailure(result.Message);
            return;
        }

        await RefreshAllAsync();
        NavigateTo(new NavigationTarget(BrowserEntityType.Certificate, result.Value.CertificateId, NavigationFocusSection.Inspector));
        NotifySuccess("Self-signed CA certificate created.");
    }

    private async Task CreateCertificateSigningRequestAsync()
    {
        if (PrivateKeysPage.SelectedItem is null)
        {
            NotifyFailure("Select a private key first.");
            return;
        }

        using var scope = BeginBusy("Creating certificate signing request");
        var result = await _databaseSessionService.CreateCertificateSigningRequestAsync(
            new CreateCertificateSigningRequestWorkflowRequest(
                PrivateKeysPage.SelectedItem.PrivateKeyId,
                PrivateKeysPage.CertificateSigningRequestDisplayName,
                PrivateKeysPage.CertificateSigningRequestSubjectName,
                ParseSubjectAlternativeNames(PrivateKeysPage.CertificateSigningRequestSubjectAlternativeNames)),
            CancellationToken.None);

        if (!result.IsSuccess || result.Value is null)
        {
            NotifyFailure(result.Message);
            return;
        }

        await RefreshAllAsync();
        NavigateTo(new NavigationTarget(BrowserEntityType.CertificateSigningRequest, result.Value.CertificateSigningRequestId, NavigationFocusSection.Overview));
        NotifySuccess("Certificate signing request created.");
    }

    private async Task SignCertificateSigningRequestAsync()
    {
        if (CertificateRequestsPage.SelectedItem is null
            || CertificateRequestsPage.SelectedIssuerCertificate is null
            || CertificateRequestsPage.SelectedIssuerPrivateKey is null)
        {
            NotifyFailure("Select a CSR, issuer certificate, and issuer private key.");
            return;
        }

        using var scope = BeginBusy("Signing certificate request");
        var result = await _databaseSessionService.SignCertificateSigningRequestAsync(
            new SignStoredCertificateSigningRequestRequest(
                CertificateRequestsPage.SelectedItem.CertificateSigningRequestId,
                CertificateRequestsPage.SelectedIssuerCertificate.CertificateId,
                CertificateRequestsPage.SelectedIssuerPrivateKey.PrivateKeyId,
                CertificateRequestsPage.IssuedCertificateDisplayName,
                Math.Max(1, CertificateRequestsPage.ValidityDays)),
            CancellationToken.None);

        if (!result.IsSuccess || result.Value is null)
        {
            NotifyFailure(result.Message);
            return;
        }

        await RefreshAllAsync();
        NavigateTo(new NavigationTarget(BrowserEntityType.Certificate, result.Value.CertificateId, NavigationFocusSection.Inspector));
        NotifySuccess("CSR signed into a certificate.");
    }

    private async Task RevokeSelectedCertificateAsync()
    {
        if (CertificatesPage.SelectedItem is null)
        {
            NotifyFailure("Select a certificate first.");
            return;
        }

        using var scope = BeginBusy("Revoking certificate");
        var result = await _databaseSessionService.RevokeCertificateAsync(
            new RevokeStoredCertificateRequest(
                CertificatesPage.SelectedItem.CertificateId,
                CertificatesPage.SelectedRevocationReason,
                CertificatesPage.SelectedRevocationDate),
            CancellationToken.None);

        if (!result.IsSuccess || result.Value is null)
        {
            NotifyFailure(result.Message);
            return;
        }

        CertificatesPage.RevocationConfirmationText = string.Empty;
        await RefreshAllAsync();
        NavigateTo(new NavigationTarget(BrowserEntityType.Certificate, result.Value.CertificateId, NavigationFocusSection.Revocation));
        NotifySuccess("Certificate revoked.");
    }

    private async Task GenerateCertificateRevocationListAsync()
    {
        if (CertificatesPage.SelectedItem?.PrivateKeyId is null)
        {
            NotifyFailure("The selected CA certificate must have an associated private key.");
            return;
        }

        using var scope = BeginBusy("Generating certificate revocation list");
        var result = await _databaseSessionService.GenerateCertificateRevocationListAsync(
            new GenerateCertificateRevocationListWorkflowRequest(
                CertificatesPage.SelectedItem.CertificateId,
                CertificatesPage.SelectedItem.PrivateKeyId.Value,
                $"{CertificatesPage.SelectedItem.DisplayName} CRL",
                7),
            CancellationToken.None);

        if (!result.IsSuccess || result.Value is null)
        {
            NotifyFailure(result.Message);
            return;
        }

        await RefreshAllAsync();
        NavigateTo(new NavigationTarget(BrowserEntityType.CertificateRevocationList, result.Value.CertificateRevocationListId, NavigationFocusSection.RevokedEntries));
        NotifySuccess("Certificate revocation list generated.");
    }

    private async Task ImportMaterialAsync()
    {
        using var scope = BeginBusy("Importing certificate material");
        var result = await _databaseSessionService.ImportStoredMaterialAsync(
            new ImportStoredMaterialRequest(
                CertificatesPage.ImportDisplayName,
                MapImportKind(CertificatesPage.SelectedImportKind),
                MapFormat(CertificatesPage.SelectedImportFormat),
                Encoding.UTF8.GetBytes(CertificatesPage.ImportPayload),
                string.IsNullOrWhiteSpace(CertificatesPage.ImportPassword) ? null : CertificatesPage.ImportPassword),
            CancellationToken.None);

        if (!result.IsSuccess || result.Value is null)
        {
            NotifyFailure(result.Message);
            return;
        }

        await RefreshAllAsync();
        if (result.Value.CertificateIds.Count > 0)
        {
            NavigateTo(new NavigationTarget(BrowserEntityType.Certificate, result.Value.CertificateIds[0], NavigationFocusSection.Inspector));
        }
        else if (result.Value.PrivateKeyIds.Count > 0)
        {
            NavigateTo(new NavigationTarget(BrowserEntityType.PrivateKey, result.Value.PrivateKeyIds[0], NavigationFocusSection.Overview));
        }
        else if (result.Value.CertificateSigningRequestIds.Count > 0)
        {
            NavigateTo(new NavigationTarget(BrowserEntityType.CertificateSigningRequest, result.Value.CertificateSigningRequestIds[0], NavigationFocusSection.Overview));
        }

        NotifySuccess("Material imported.");
    }

    private async Task ExportSelectedCertificateAsync()
    {
        if (!CertificatesPage.HasSelection || CertificatesPage.SelectedItem is null)
        {
            NotifyFailure("Select a certificate first.");
            return;
        }

        using var scope = BeginBusy("Exporting certificate");
        var result = await _databaseSessionService.ExportStoredMaterialAsync(
            new ExportStoredMaterialRequest(
                CryptoImportKind.Certificate,
                CertificatesPage.SelectedItem.CertificateId,
                MapFormat(CertificatesPage.SelectedExportFormat),
                string.IsNullOrWhiteSpace(CertificatesPage.SelectedExportPassword) ? null : CertificatesPage.SelectedExportPassword,
                "xcanet-certificate"),
            CancellationToken.None);

        ApplyExportResult(result, value => CertificatesPage.ExportPreview = value, "Certificate exported.");
    }

    private async Task ExportSelectedPrivateKeyAsync()
    {
        if (!PrivateKeysPage.HasSelection || PrivateKeysPage.SelectedItem is null)
        {
            NotifyFailure("Select a private key first.");
            return;
        }

        using var scope = BeginBusy("Exporting private key");
        var result = await _databaseSessionService.ExportStoredMaterialAsync(
            new ExportStoredMaterialRequest(
                CryptoImportKind.PrivateKey,
                PrivateKeysPage.SelectedItem.PrivateKeyId,
                MapFormat(PrivateKeysPage.SelectedExportFormat),
                string.IsNullOrWhiteSpace(PrivateKeysPage.SelectedExportPassword) ? null : PrivateKeysPage.SelectedExportPassword,
                "xcanet-private-key"),
            CancellationToken.None);

        ApplyExportResult(result, value => PrivateKeysPage.ExportPreview = value, "Private key exported.");
    }

    private async Task ExportSelectedCertificateSigningRequestAsync()
    {
        if (!CertificateRequestsPage.HasSelection || CertificateRequestsPage.SelectedItem is null)
        {
            NotifyFailure("Select a CSR first.");
            return;
        }

        using var scope = BeginBusy("Exporting certificate request");
        var result = await _databaseSessionService.ExportStoredMaterialAsync(
            new ExportStoredMaterialRequest(
                CryptoImportKind.CertificateSigningRequest,
                CertificateRequestsPage.SelectedItem.CertificateSigningRequestId,
                MapFormat(CertificateRequestsPage.SelectedExportFormat),
                null,
                "xcanet-csr"),
            CancellationToken.None);

        ApplyExportResult(result, value => CertificateRequestsPage.ExportPreview = value, "Certificate signing request exported.");
    }

    private async Task RefreshAllAsync()
    {
        using var scope = BeginBusy("Refreshing workspace");
        ApplySnapshot(Snapshot);

        if (Snapshot.State == DatabaseSessionState.Closed || string.IsNullOrWhiteSpace(Snapshot.DatabasePath))
        {
            ClearBrowseState();
            return;
        }

        await LoadDashboardAsync();
        await LoadPrivateKeysAsync();
        await LoadCertificatesAsync();
        await LoadCertificateRequestsAsync();
        await LoadCertificateRevocationListsAsync();
        await LoadTemplatesAsync();
    }

    private async Task LoadDashboardAsync()
    {
        var summary = await _databaseSessionService.GetDashboardSummaryAsync(CancellationToken.None);
        if (!summary.IsSuccess || summary.Value is null)
        {
            DashboardPage.CertificateCount = 0;
            DashboardPage.PrivateKeyCount = 0;
            DashboardPage.CertificateRequestCount = 0;
            DashboardPage.CertificateRevocationListCount = 0;
            DashboardPage.TemplateCount = 0;
            return;
        }

        DashboardPage.CertificateCount = summary.Value.Certificates;
        DashboardPage.PrivateKeyCount = summary.Value.PrivateKeys;
        DashboardPage.CertificateRequestCount = summary.Value.CertificateSigningRequests;
        DashboardPage.CertificateRevocationListCount = summary.Value.CertificateRevocationLists;
        DashboardPage.TemplateCount = summary.Value.Templates;
    }

    private async Task LoadCertificatesAsync()
    {
        if (Snapshot.State == DatabaseSessionState.Closed)
        {
            CertificatesPage.SetItems([]);
            CertificatesPage.Inspector = null;
            CertificatesPage.SelectedChildNavigationItem = null;
            return;
        }

        var result = await _databaseSessionService.ListCertificatesAsync(CertificatesPage.Filter, CancellationToken.None);
        CertificatesPage.SetItems(result.IsSuccess && result.Value is not null ? result.Value : []);
        await LoadSelectedCertificateInspectorAsync();
        CertificateRequestsPage.SetIssuers(CertificatesPage.Items, PrivateKeysPage.Items);
    }

    private async Task LoadPrivateKeysAsync()
    {
        if (Snapshot.State == DatabaseSessionState.Closed)
        {
            PrivateKeysPage.SetItems([]);
            return;
        }

        var result = await _databaseSessionService.ListPrivateKeysAsync(CancellationToken.None);
        PrivateKeysPage.SetItems(result.IsSuccess && result.Value is not null ? result.Value : []);
        CertificateRequestsPage.SetIssuers(CertificatesPage.Items, PrivateKeysPage.Items);
    }

    private async Task LoadCertificateRequestsAsync()
    {
        if (Snapshot.State == DatabaseSessionState.Closed)
        {
            CertificateRequestsPage.SetItems([]);
            return;
        }

        var result = await _databaseSessionService.ListCertificateSigningRequestsAsync(CancellationToken.None);
        CertificateRequestsPage.SetItems(result.IsSuccess && result.Value is not null ? result.Value : []);
    }

    private async Task LoadCertificateRevocationListsAsync()
    {
        if (Snapshot.State == DatabaseSessionState.Closed)
        {
            CertificateRevocationListsPage.SetItems([]);
            CertificateRevocationListsPage.Inspector = null;
            return;
        }

        var result = await _databaseSessionService.ListCertificateRevocationListsAsync(CancellationToken.None);
        CertificateRevocationListsPage.SetItems(result.IsSuccess && result.Value is not null ? result.Value : []);
        await LoadSelectedCertificateRevocationListInspectorAsync();
    }

    private async Task LoadTemplatesAsync()
    {
        if (Snapshot.State == DatabaseSessionState.Closed)
        {
            TemplatesPage.SetItems([]);
            return;
        }

        var result = await _databaseSessionService.ListTemplatesAsync(CancellationToken.None);
        TemplatesPage.SetItems(result.IsSuccess && result.Value is not null ? result.Value : []);
    }

    private async Task LoadSelectedCertificateInspectorAsync()
    {
        if (!CertificatesPage.HasSelection || CertificatesPage.SelectedItem is null || Snapshot.State == DatabaseSessionState.Closed)
        {
            CertificatesPage.Inspector = null;
            CertificatesPage.SelectedChildNavigationItem = null;
            RefreshCommandStates();
            return;
        }

        var result = await _databaseSessionService.GetCertificateInspectorAsync(CertificatesPage.SelectedItem.CertificateId, CancellationToken.None);
        CertificatesPage.Inspector = result.IsSuccess ? result.Value : null;
        CertificatesPage.SelectedChildNavigationItem = CertificatesPage.Inspector?.Navigation.Children.FirstOrDefault();
        RefreshCommandStates();
    }

    private async Task LoadSelectedCertificateRevocationListInspectorAsync()
    {
        if (!CertificateRevocationListsPage.HasSelection || CertificateRevocationListsPage.SelectedItem is null || Snapshot.State == DatabaseSessionState.Closed)
        {
            CertificateRevocationListsPage.Inspector = null;
            RefreshCommandStates();
            return;
        }

        var result = await _databaseSessionService.GetCertificateRevocationListInspectorAsync(CertificateRevocationListsPage.SelectedItem.CertificateRevocationListId, CancellationToken.None);
        CertificateRevocationListsPage.Inspector = result.IsSuccess ? result.Value : null;
        RefreshCommandStates();
    }

    private void NavigateTo(NavigationTarget? target)
    {
        if (target is null)
        {
            return;
        }

        switch (target.EntityType)
        {
            case BrowserEntityType.Certificate:
                SelectPage(CertificatesPage);
                CertificatesPage.SelectedItem = CertificatesPage.Items.FirstOrDefault(x => x.CertificateId == target.EntityId);
                break;
            case BrowserEntityType.PrivateKey:
                SelectPage(PrivateKeysPage);
                PrivateKeysPage.SelectedItem = PrivateKeysPage.Items.FirstOrDefault(x => x.PrivateKeyId == target.EntityId);
                break;
            case BrowserEntityType.CertificateSigningRequest:
                SelectPage(CertificateRequestsPage);
                CertificateRequestsPage.SelectedItem = CertificateRequestsPage.Items.FirstOrDefault(x => x.CertificateSigningRequestId == target.EntityId);
                break;
            case BrowserEntityType.CertificateRevocationList:
                SelectPage(CertificateRevocationListsPage);
                CertificateRevocationListsPage.SelectedItem = CertificateRevocationListsPage.Items.FirstOrDefault(x => x.CertificateRevocationListId == target.EntityId);
                break;
            case BrowserEntityType.Template:
                SelectPage(TemplatesPage);
                break;
        }
    }

    private void SelectPage(PageViewModelBase page)
    {
        CurrentPage = page;
        foreach (var item in NavigationItems)
        {
            item.IsSelected = item.Title == page.Title;
        }
    }

    private void OnCertificatesPagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CertificatesPageViewModel.SelectedItem)
            or nameof(CertificatesPageViewModel.Filter)
            or nameof(CertificatesPageViewModel.RevocationConfirmationText)
            or nameof(CertificatesPageViewModel.SelectedChildNavigationItem))
        {
            if (e.PropertyName == nameof(CertificatesPageViewModel.SelectedItem))
            {
                _ = LoadSelectedCertificateInspectorAsync();
            }

            RefreshCommandStates();
        }
    }

    private void OnPageSelectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PrivateKeysPageViewModel.SelectedItem)
            or nameof(CertificateRequestsPageViewModel.SelectedItem)
            or nameof(CertificateRequestsPageViewModel.SelectedIssuerCertificate)
            or nameof(CertificateRequestsPageViewModel.SelectedIssuerPrivateKey)
            or nameof(CertificateRevocationListsPageViewModel.SelectedItem))
        {
            if (sender == CertificateRevocationListsPage && e.PropertyName == nameof(CertificateRevocationListsPageViewModel.SelectedItem))
            {
                _ = LoadSelectedCertificateRevocationListInspectorAsync();
            }

            RefreshCommandStates();
        }
    }

    private async Task RunDatabaseActionAsync(string busyMessage, Func<Task<OperationResult<DatabaseSessionSnapshot>>> action)
    {
        using var scope = BeginBusy(busyMessage);
        var result = await action();
        ApplySnapshot(Snapshot);
        if (!result.IsSuccess)
        {
            NotifyFailure(result.Message);
            return;
        }

        await RefreshAllAsync();
        NotifySuccess(result.Message);
    }

    private void ApplySnapshot(DatabaseSessionSnapshot snapshot)
    {
        Subtitle = snapshot.State switch
        {
            DatabaseSessionState.Unlocked => "Database unlocked",
            DatabaseSessionState.Locked => "Database open (locked)",
            _ => "Core UI workflows"
        };

        SettingsSecurityPage.StatusMessage = snapshot.StatusMessage;
        SettingsSecurityPage.SessionState = snapshot.State.ToString();
        SettingsSecurityPage.LastOpened = snapshot.LastOpenedUtc?.ToString("u") ?? "Never";

        DashboardPage.SessionState = snapshot.StatusMessage;
        DashboardPage.DatabaseDisplayName = snapshot.DisplayName ?? "No database selected";
        DashboardPage.DatabasePath = snapshot.DatabasePath ?? "Open or create a database to begin.";

        RefreshCommandStates();
    }

    private void ClearBrowseState()
    {
        DashboardPage.CertificateCount = 0;
        DashboardPage.PrivateKeyCount = 0;
        DashboardPage.CertificateRequestCount = 0;
        DashboardPage.CertificateRevocationListCount = 0;
        DashboardPage.TemplateCount = 0;
        CertificatesPage.SetItems([]);
        CertificatesPage.Inspector = null;
        CertificatesPage.SelectedChildNavigationItem = null;
        PrivateKeysPage.SetItems([]);
        CertificateRequestsPage.SetItems([]);
        CertificateRequestsPage.SetIssuers([], []);
        CertificateRevocationListsPage.SetItems([]);
        CertificateRevocationListsPage.Inspector = null;
        TemplatesPage.SetItems([]);
        RefreshCommandStates();
    }

    private IDisposable BeginBusy(string message)
    {
        IsBusy = true;
        BusyMessage = message;
        return new BusyScope(() =>
        {
            BusyMessage = string.Empty;
            IsBusy = false;
        });
    }

    private void ApplyExportResult(OperationResult<ExportedArtifact> result, Action<string> assignPreview, string successMessage)
    {
        if (!result.IsSuccess || result.Value is null)
        {
            NotifyFailure(result.Message);
            return;
        }

        assignPreview(result.Value.TextRepresentation ?? Convert.ToBase64String(result.Value.Data));
        NotifySuccess(successMessage);
    }

    private bool CanRevokeSelectedCertificate()
    {
        return !IsBusy
            && Snapshot.State == DatabaseSessionState.Unlocked
            && CertificatesPage.SelectedItem is { } certificate
            && !string.Equals(certificate.RevocationStatus, "Revoked", StringComparison.OrdinalIgnoreCase)
            && CertificatesPage.IsRevocationConfirmed;
    }

    private bool CanGenerateCertificateRevocationList()
    {
        return !IsBusy
            && Snapshot.State == DatabaseSessionState.Unlocked
            && CertificatesPage.SelectedItem is { IsCertificateAuthority: true, PrivateKeyId: not null } certificate
            && !string.Equals(certificate.RevocationStatus, "Revoked", StringComparison.OrdinalIgnoreCase);
    }

    private void NotifySuccess(string message) => AddNotification("Success", message);

    private void NotifyFailure(string message) => AddNotification("Error", message);

    private void AddNotification(string level, string message)
    {
        SettingsSecurityPage.StatusMessage = message;
        Notifications.Insert(0, new NotificationItemViewModel(level, message));
        while (Notifications.Count > 6)
        {
            Notifications.RemoveAt(Notifications.Count - 1);
        }
    }

    private void RefreshCommandStates()
    {
        _createDatabaseCommand.RaiseCanExecuteChanged();
        _openDatabaseCommand.RaiseCanExecuteChanged();
        _unlockDatabaseCommand.RaiseCanExecuteChanged();
        _lockDatabaseCommand.RaiseCanExecuteChanged();
        _refreshWorkspaceCommand.RaiseCanExecuteChanged();
        _refreshCertificatesCommand.RaiseCanExecuteChanged();
        _importMaterialCommand.RaiseCanExecuteChanged();
        _exportCertificateCommand.RaiseCanExecuteChanged();
        _revokeCertificateCommand.RaiseCanExecuteChanged();
        _generateCertificateRevocationListCommand.RaiseCanExecuteChanged();
        _navigateIssuerCommand.RaiseCanExecuteChanged();
        _navigatePrivateKeyCommand.RaiseCanExecuteChanged();
        _navigateChildCertificateCommand.RaiseCanExecuteChanged();
        _refreshPrivateKeysCommand.RaiseCanExecuteChanged();
        _generateKeyCommand.RaiseCanExecuteChanged();
        _createSelfSignedCaCommand.RaiseCanExecuteChanged();
        _createCertificateSigningRequestCommand.RaiseCanExecuteChanged();
        _exportPrivateKeyCommand.RaiseCanExecuteChanged();
        _refreshCertificateRequestsCommand.RaiseCanExecuteChanged();
        _signCertificateSigningRequestCommand.RaiseCanExecuteChanged();
        _exportCertificateSigningRequestCommand.RaiseCanExecuteChanged();
        _navigateRequestPrivateKeyCommand.RaiseCanExecuteChanged();
        _refreshCertificateRevocationListsCommand.RaiseCanExecuteChanged();
        _navigateCrlIssuerCommand.RaiseCanExecuteChanged();
        _refreshTemplatesCommand.RaiseCanExecuteChanged();
    }

    private static IReadOnlyList<SanEntry> ParseSubjectAlternativeNames(string value)
    {
        return value
            .Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => new SanEntry(x))
            .ToList();
    }

    private static CryptoImportKind MapImportKind(CryptoImportKindView value)
    {
        return value switch
        {
            CryptoImportKindView.PrivateKey => CryptoImportKind.PrivateKey,
            CryptoImportKindView.CertificateSigningRequest => CryptoImportKind.CertificateSigningRequest,
            _ => CryptoImportKind.Certificate
        };
    }

    private static CryptoDataFormat MapFormat(CryptoFormatView value)
    {
        return value switch
        {
            CryptoFormatView.Der => CryptoDataFormat.Der,
            CryptoFormatView.Pkcs8 => CryptoDataFormat.Pkcs8,
            CryptoFormatView.Pkcs10 => CryptoDataFormat.Pkcs10,
            CryptoFormatView.Pkcs12 => CryptoDataFormat.Pkcs12,
            _ => CryptoDataFormat.Pem
        };
    }

    private sealed class BusyScope : IDisposable
    {
        private readonly Action _dispose;
        private bool _disposed;

        public BusyScope(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _dispose();
        }
    }
}
