using System.Collections.ObjectModel;
using System.Text;
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
using Microsoft.Extensions.Logging;

namespace XcaNet.App.ViewModels;

public sealed class ShellViewModel : ViewModelBase
{
    private readonly IDatabaseSessionService _databaseSessionService;
    private readonly ILogger<ShellViewModel> _logger;

    private readonly AsyncCommand _createDatabaseCommand;
    private readonly AsyncCommand _openDatabaseCommand;
    private readonly AsyncCommand _unlockDatabaseCommand;
    private readonly AsyncCommand _lockDatabaseCommand;
    private readonly AsyncCommand _refreshDashboardCommand;
    private readonly AsyncCommand _refreshCertificatesCommand;
    private readonly AsyncCommand _importMaterialCommand;
    private readonly AsyncCommand _exportCertificateCommand;
    private readonly DelegateCommand _openIssuerCommand;
    private readonly DelegateCommand _openPrivateKeyFromCertificateCommand;
    private readonly DelegateCommand _openChildCertificateCommand;
    private readonly AsyncCommand _refreshPrivateKeysCommand;
    private readonly AsyncCommand _generateKeyCommand;
    private readonly AsyncCommand _createSelfSignedCaCommand;
    private readonly AsyncCommand _createCertificateSigningRequestCommand;
    private readonly AsyncCommand _exportPrivateKeyCommand;
    private readonly AsyncCommand _refreshCertificateRequestsCommand;
    private readonly AsyncCommand _signCertificateSigningRequestCommand;
    private readonly AsyncCommand _exportCertificateSigningRequestCommand;
    private readonly DelegateCommand _openPrivateKeyFromRequestCommand;
    private readonly AsyncCommand _refreshCertificateRevocationListsCommand;
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
        _refreshDashboardCommand = new AsyncCommand(RefreshAllAsync, () => !IsBusy);
        _refreshCertificatesCommand = new AsyncCommand(LoadCertificatesAsync, () => !IsBusy && Snapshot.State != DatabaseSessionState.Closed);
        _importMaterialCommand = new AsyncCommand(ImportMaterialAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked);
        _exportCertificateCommand = new AsyncCommand(ExportSelectedCertificateAsync, () => !IsBusy && CertificatesPage.SelectedItem is not null && Snapshot.State == DatabaseSessionState.Unlocked);
        _openIssuerCommand = new DelegateCommand(OpenIssuerCertificate, () => CertificatesPage.Inspector.CanOpenIssuer);
        _openPrivateKeyFromCertificateCommand = new DelegateCommand(OpenPrivateKeyFromCertificate, () => CertificatesPage.Inspector.CanOpenPrivateKey);
        _openChildCertificateCommand = new DelegateCommand(OpenChildCertificate, () => CertificatesPage.Inspector.CanOpenSelectedChild);
        _refreshPrivateKeysCommand = new AsyncCommand(LoadPrivateKeysAsync, () => !IsBusy && Snapshot.State != DatabaseSessionState.Closed);
        _generateKeyCommand = new AsyncCommand(GenerateKeyAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked);
        _createSelfSignedCaCommand = new AsyncCommand(CreateSelfSignedCaAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked && PrivateKeysPage.SelectedItem is not null);
        _createCertificateSigningRequestCommand = new AsyncCommand(CreateCertificateSigningRequestAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked && PrivateKeysPage.SelectedItem is not null);
        _exportPrivateKeyCommand = new AsyncCommand(ExportSelectedPrivateKeyAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked && PrivateKeysPage.SelectedItem is not null);
        _refreshCertificateRequestsCommand = new AsyncCommand(LoadCertificateRequestsAsync, () => !IsBusy && Snapshot.State != DatabaseSessionState.Closed);
        _signCertificateSigningRequestCommand = new AsyncCommand(SignCertificateSigningRequestAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked && CertificateRequestsPage.SelectedItem is not null && CertificateRequestsPage.SelectedIssuerCertificate is not null && CertificateRequestsPage.SelectedIssuerPrivateKey is not null);
        _exportCertificateSigningRequestCommand = new AsyncCommand(ExportSelectedCertificateSigningRequestAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked && CertificateRequestsPage.SelectedItem is not null);
        _openPrivateKeyFromRequestCommand = new DelegateCommand(OpenPrivateKeyFromRequest, () => CertificateRequestsPage.SelectedItem?.PrivateKeyId is not null);
        _refreshCertificateRevocationListsCommand = new AsyncCommand(LoadCertificateRevocationListsAsync, () => !IsBusy && Snapshot.State != DatabaseSessionState.Closed);
        _refreshTemplatesCommand = new AsyncCommand(LoadTemplatesAsync, () => !IsBusy && Snapshot.State != DatabaseSessionState.Closed);

        SettingsSecurityPage.CreateDatabaseCommand = _createDatabaseCommand;
        SettingsSecurityPage.OpenDatabaseCommand = _openDatabaseCommand;
        SettingsSecurityPage.UnlockDatabaseCommand = _unlockDatabaseCommand;
        SettingsSecurityPage.LockDatabaseCommand = _lockDatabaseCommand;

        CertificatesPage.RefreshCommand = _refreshCertificatesCommand;
        CertificatesPage.ImportMaterialCommand = _importMaterialCommand;
        CertificatesPage.ExportSelectedCommand = _exportCertificateCommand;
        CertificatesPage.OpenIssuerCommand = _openIssuerCommand;
        CertificatesPage.OpenPrivateKeyCommand = _openPrivateKeyFromCertificateCommand;
        CertificatesPage.OpenChildCertificateCommand = _openChildCertificateCommand;

        PrivateKeysPage.RefreshCommand = _refreshPrivateKeysCommand;
        PrivateKeysPage.GenerateKeyCommand = _generateKeyCommand;
        PrivateKeysPage.CreateSelfSignedCaCommand = _createSelfSignedCaCommand;
        PrivateKeysPage.CreateCertificateSigningRequestCommand = _createCertificateSigningRequestCommand;
        PrivateKeysPage.ExportSelectedCommand = _exportPrivateKeyCommand;

        CertificateRequestsPage.RefreshCommand = _refreshCertificateRequestsCommand;
        CertificateRequestsPage.SignSelectedCommand = _signCertificateSigningRequestCommand;
        CertificateRequestsPage.ExportSelectedCommand = _exportCertificateSigningRequestCommand;
        CertificateRequestsPage.OpenSelectedPrivateKeyCommand = _openPrivateKeyFromRequestCommand;

        CertificateRevocationListsPage.RefreshCommand = _refreshCertificateRevocationListsCommand;
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
        CertificatesPage.Inspector.PropertyChanged += (_, _) => RefreshCommandStates();
        PrivateKeysPage.PropertyChanged += (_, _) => RefreshCommandStates();
        CertificateRequestsPage.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(CertificateRequestsPageViewModel.SelectedItem)
                or nameof(CertificateRequestsPageViewModel.SelectedIssuerCertificate)
                or nameof(CertificateRequestsPageViewModel.SelectedIssuerPrivateKey))
            {
                RefreshCommandStates();
            }
        };

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
            () => _databaseSessionService.OpenDatabaseAsync(
                new OpenDatabaseRequest(SettingsSecurityPage.DatabasePath),
                CancellationToken.None));
    }

    private async Task UnlockDatabaseAsync()
    {
        await RunDatabaseActionAsync(
            "Unlocking database",
            () => _databaseSessionService.UnlockDatabaseAsync(
                new UnlockDatabaseRequest(SettingsSecurityPage.Password),
                CancellationToken.None));
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
            new GenerateStoredKeyRequest(
                PrivateKeysPage.NewKeyDisplayName,
                algorithm,
                algorithm == KeyAlgorithmKind.Rsa ? 3072 : null,
                curve),
            CancellationToken.None);

        if (!result.IsSuccess || result.Value is null)
        {
            NotifyFailure(result.Message);
            return;
        }

        await RefreshAllAsync();
        SelectPage(PrivateKeysPage);
        SelectPrivateKey(result.Value.PrivateKeyId);
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
        SelectCertificate(result.Value.CertificateId);
        SelectPage(CertificatesPage);
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
        SelectCertificateRequest(result.Value.CertificateSigningRequestId);
        SelectPage(CertificateRequestsPage);
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
        SelectCertificate(result.Value.CertificateId);
        SelectPage(CertificatesPage);
        NotifySuccess("CSR signed into a certificate.");
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
            SelectCertificate(result.Value.CertificateIds[0]);
        }
        else if (result.Value.PrivateKeyIds.Count > 0)
        {
            SelectPrivateKey(result.Value.PrivateKeyIds[0]);
        }
        else if (result.Value.CertificateSigningRequestIds.Count > 0)
        {
            SelectCertificateRequest(result.Value.CertificateSigningRequestIds[0]);
        }

        NotifySuccess("Material imported.");
    }

    private async Task ExportSelectedCertificateAsync()
    {
        if (CertificatesPage.SelectedItem is null)
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
        if (PrivateKeysPage.SelectedItem is null)
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
        if (CertificateRequestsPage.SelectedItem is null)
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
        await LoadCertificatesAsync();
        await LoadPrivateKeysAsync();
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
            CertificatesPage.Inspector.Clear();
            return;
        }

        var result = await _databaseSessionService.ListCertificatesAsync(
            new CertificateBrowserQuery(
                CertificatesPage.SearchText,
                CertificatesPage.SelectedValidityFilter,
                CertificatesPage.SelectedAuthorityFilter,
                30),
            CancellationToken.None);

        if (!result.IsSuccess || result.Value is null)
        {
            CertificatesPage.SetItems([]);
            CertificatesPage.Inspector.Clear();
            return;
        }

        CertificatesPage.SetItems(result.Value);
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
            return;
        }

        var result = await _databaseSessionService.ListCertificateRevocationListsAsync(CancellationToken.None);
        CertificateRevocationListsPage.SetItems(result.IsSuccess && result.Value is not null ? result.Value : []);
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
        if (CertificatesPage.SelectedItem is null || Snapshot.State == DatabaseSessionState.Closed)
        {
            CertificatesPage.Inspector.Clear();
            return;
        }

        var result = await _databaseSessionService.GetCertificateInspectorAsync(CertificatesPage.SelectedItem.CertificateId, CancellationToken.None);
        if (!result.IsSuccess || result.Value is null)
        {
            CertificatesPage.Inspector.Clear();
            return;
        }

        CertificatesPage.Inspector.Apply(result.Value);
    }

    private void OpenIssuerCertificate()
    {
        if (CertificatesPage.Inspector.IssuerCertificateId is null)
        {
            return;
        }

        SelectCertificate(CertificatesPage.Inspector.IssuerCertificateId.Value);
        SelectPage(CertificatesPage);
    }

    private void OpenPrivateKeyFromCertificate()
    {
        if (CertificatesPage.Inspector.PrivateKeyId is null)
        {
            return;
        }

        SelectPrivateKey(CertificatesPage.Inspector.PrivateKeyId.Value);
        SelectPage(PrivateKeysPage);
    }

    private void OpenChildCertificate()
    {
        if (CertificatesPage.Inspector.SelectedChildCertificate is null)
        {
            return;
        }

        SelectCertificate(CertificatesPage.Inspector.SelectedChildCertificate.CertificateId);
        SelectPage(CertificatesPage);
    }

    private void OpenPrivateKeyFromRequest()
    {
        if (CertificateRequestsPage.SelectedItem?.PrivateKeyId is null)
        {
            return;
        }

        SelectPrivateKey(CertificateRequestsPage.SelectedItem.PrivateKeyId.Value);
        SelectPage(PrivateKeysPage);
    }

    private void SelectPage(PageViewModelBase page)
    {
        CurrentPage = page;
        foreach (var item in NavigationItems)
        {
            item.IsSelected = item.Title == page.Title;
        }
    }

    private void SelectCertificate(Guid certificateId)
    {
        CertificatesPage.SelectedItem = CertificatesPage.Items.FirstOrDefault(x => x.CertificateId == certificateId);
    }

    private void SelectPrivateKey(Guid privateKeyId)
    {
        PrivateKeysPage.SelectedItem = PrivateKeysPage.Items.FirstOrDefault(x => x.PrivateKeyId == privateKeyId);
    }

    private void SelectCertificateRequest(Guid certificateSigningRequestId)
    {
        CertificateRequestsPage.SelectedItem = CertificateRequestsPage.Items.FirstOrDefault(x => x.CertificateSigningRequestId == certificateSigningRequestId);
    }

    private void OnCertificatesPagePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CertificatesPageViewModel.SelectedItem))
        {
            _ = LoadSelectedCertificateInspectorAsync();
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
        CertificatesPage.Inspector.Clear();
        PrivateKeysPage.SetItems([]);
        CertificateRequestsPage.SetItems([]);
        CertificateRequestsPage.SetIssuers([], []);
        CertificateRevocationListsPage.SetItems([]);
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

    private void NotifySuccess(string message)
    {
        AddNotification("Success", message);
    }

    private void NotifyFailure(string message)
    {
        AddNotification("Error", message);
    }

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
        _refreshDashboardCommand.RaiseCanExecuteChanged();
        _refreshCertificatesCommand.RaiseCanExecuteChanged();
        _importMaterialCommand.RaiseCanExecuteChanged();
        _exportCertificateCommand.RaiseCanExecuteChanged();
        _openIssuerCommand.RaiseCanExecuteChanged();
        _openPrivateKeyFromCertificateCommand.RaiseCanExecuteChanged();
        _openChildCertificateCommand.RaiseCanExecuteChanged();
        _refreshPrivateKeysCommand.RaiseCanExecuteChanged();
        _generateKeyCommand.RaiseCanExecuteChanged();
        _createSelfSignedCaCommand.RaiseCanExecuteChanged();
        _createCertificateSigningRequestCommand.RaiseCanExecuteChanged();
        _exportPrivateKeyCommand.RaiseCanExecuteChanged();
        _refreshCertificateRequestsCommand.RaiseCanExecuteChanged();
        _signCertificateSigningRequestCommand.RaiseCanExecuteChanged();
        _exportCertificateSigningRequestCommand.RaiseCanExecuteChanged();
        _openPrivateKeyFromRequestCommand.RaiseCanExecuteChanged();
        _refreshCertificateRevocationListsCommand.RaiseCanExecuteChanged();
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
