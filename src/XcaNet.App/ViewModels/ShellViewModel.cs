using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using XcaNet.App.Commands;
using XcaNet.App.Services;
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
    private readonly IDesktopFileDialogService _fileDialogService;
    private readonly ILogger<ShellViewModel> _logger;

    private readonly AsyncCommand _createDatabaseCommand;
    private readonly AsyncCommand _openDatabaseCommand;
    private readonly AsyncCommand _unlockDatabaseCommand;
    private readonly AsyncCommand _lockDatabaseCommand;
    private readonly AsyncCommand _closeDatabaseCommand;
    private readonly AsyncCommand _refreshWorkspaceCommand;
    private readonly DelegateCommand _exitCommand;
    private readonly AsyncCommand _refreshCertificatesCommand;
    private readonly AsyncCommand _importFilesCommand;
    private readonly AsyncCommand _importMaterialCommand;
    private readonly AsyncCommand _exportCertificateCommand;
    private readonly AsyncCommand _exportCertificateToFileCommand;
    private readonly AsyncCommand _revokeCertificateCommand;
    private readonly AsyncCommand _generateCertificateRevocationListCommand;
    private readonly DelegateCommand _navigateIssuerCommand;
    private readonly DelegateCommand _navigatePrivateKeyCommand;
    private readonly DelegateCommand _navigateChildCertificateCommand;
    private readonly AsyncCommand _refreshPrivateKeysCommand;
    private readonly AsyncCommand _generateKeyCommand;
    private readonly DelegateCommand _openSelfSignedCaAuthoringCommand;
    private readonly DelegateCommand _openCertificateSigningRequestAuthoringCommand;
    private readonly AsyncCommand _createSelfSignedCaCommand;
    private readonly AsyncCommand _createCertificateSigningRequestCommand;
    private readonly AsyncCommand _exportPrivateKeyCommand;
    private readonly AsyncCommand _exportPrivateKeyToFileCommand;
    private readonly AsyncCommand _refreshCertificateRequestsCommand;
    private readonly DelegateCommand _openIssuanceAuthoringCommand;
    private readonly AsyncCommand _signCertificateSigningRequestCommand;
    private readonly AsyncCommand _exportCertificateSigningRequestCommand;
    private readonly AsyncCommand _exportCertificateSigningRequestToFileCommand;
    private readonly DelegateCommand _navigateRequestPrivateKeyCommand;
    private readonly AsyncCommand _refreshCertificateRevocationListsCommand;
    private readonly AsyncCommand _exportCertificateRevocationListToFileCommand;
    private readonly DelegateCommand _navigateCrlIssuerCommand;
    private readonly AsyncCommand _refreshTemplatesCommand;
    private readonly AsyncCommand _createTemplateCommand;
    private readonly DelegateCommand _editTemplateCommand;
    private readonly AsyncCommand _saveTemplateCommand;
    private readonly AsyncCommand _cloneTemplateCommand;
    private readonly AsyncCommand _toggleTemplateFavoriteCommand;
    private readonly AsyncCommand _toggleTemplateEnabledCommand;
    private readonly AsyncCommand _deleteTemplateCommand;
    private readonly AsyncCommand _applySelfSignedCaTemplateCommand;
    private readonly AsyncCommand _applyCertificateSigningRequestTemplateCommand;
    private readonly AsyncCommand _applyIssuanceTemplateCommand;
    private readonly DelegateCommand _createTemplateFromCertificateCommand;
    private readonly DelegateCommand _createTemplateFromRequestCommand;
    private readonly DelegateCommand _createSimilarRequestCommand;
    private readonly DelegateCommand _closeAuthoringDialogCommand;
    private readonly DelegateCommand _showCertificateDetailsCommand;
    private readonly DelegateCommand _closeCertificateDetailCommand;
    private readonly DelegateCommand _openRevokeDialogCommand;
    private readonly DelegateCommand _closeRevokeDialogCommand;
    private readonly DelegateCommand _togglePlainViewCommand;

    // M15 commands
    private readonly DelegateCommand _openAboutCommand;
    private readonly DelegateCommand _closeAboutCommand;
    private readonly DelegateCommand _openOidResolverCommand;
    private readonly DelegateCommand _closeOidResolverCommand;
    private readonly DelegateCommand _resolveOidCommand;
    private readonly DelegateCommand _openPasswordChangeCommand;
    private readonly DelegateCommand _closePasswordChangeCommand;
    private readonly AsyncCommand _confirmPasswordChangeCommand;
    private readonly AsyncCommand _pastePemCommand;

    private PageViewModelBase _currentPage;
    private string _subtitle = "Core UI workflows";
    private bool _isBusy;
    private string _busyMessage = string.Empty;
    private string _searchText = string.Empty;
    private AuthoringDialogKind _authoringDialogKind;
    private CertificateAuthoringViewModel? _activeCertificateAuthoring;
    private string _authoringDialogTitle = string.Empty;
    private string _authoringDialogSubtitle = string.Empty;

    // M15 dialog state
    private bool _isAboutDialogOpen;
    private bool _isOidResolverDialogOpen;
    private bool _isPasswordChangeDialogOpen;
    private string _oidResolverInput = string.Empty;
    private string _oidResolverResult = string.Empty;
    private string _passwordChangeNew = string.Empty;
    private string _passwordChangeConfirm = string.Empty;

    public ShellViewModel(IDatabaseSessionService databaseSessionService, IDesktopFileDialogService fileDialogService, ILogger<ShellViewModel> logger)
    {
        _databaseSessionService = databaseSessionService;
        _fileDialogService = fileDialogService;
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
        _closeDatabaseCommand = new AsyncCommand(CloseDatabaseAsync, () => !IsBusy && Snapshot.State != DatabaseSessionState.Closed);
        _refreshWorkspaceCommand = new AsyncCommand(RefreshAllAsync, () => !IsBusy);
        _exitCommand = new DelegateCommand(() => Environment.Exit(0));
        _refreshCertificatesCommand = new AsyncCommand(LoadCertificatesAsync, () => !IsBusy && Snapshot.State != DatabaseSessionState.Closed);
        _importFilesCommand = new AsyncCommand(ImportFilesFromPickerAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked);
        _importMaterialCommand = new AsyncCommand(ImportMaterialAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked);
        _exportCertificateCommand = new AsyncCommand(ExportSelectedCertificateAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked && CertificatesPage.HasSelection);
        _exportCertificateToFileCommand = new AsyncCommand(ExportSelectedCertificateToFileAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked && CertificatesPage.HasSelection);
        _revokeCertificateCommand = new AsyncCommand(RevokeSelectedCertificateAsync, CanRevokeSelectedCertificate);
        _generateCertificateRevocationListCommand = new AsyncCommand(GenerateCertificateRevocationListAsync, CanGenerateCertificateRevocationList);
        _navigateIssuerCommand = new DelegateCommand(() => NavigateTo(CertificatesPage.Inspector?.Navigation.Issuer), () => CertificatesPage.Inspector?.Navigation.Issuer is not null);
        _navigatePrivateKeyCommand = new DelegateCommand(() => NavigateTo(CertificatesPage.Inspector?.Navigation.PrivateKey), () => CertificatesPage.Inspector?.Navigation.PrivateKey is not null);
        _navigateChildCertificateCommand = new DelegateCommand(() => NavigateTo(CertificatesPage.SelectedChildNavigationItem?.Target), () => CertificatesPage.SelectedChildNavigationItem is not null);
        _refreshPrivateKeysCommand = new AsyncCommand(LoadPrivateKeysAsync, () => !IsBusy && Snapshot.State != DatabaseSessionState.Closed);
        _generateKeyCommand = new AsyncCommand(GenerateKeyAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked);
        _openSelfSignedCaAuthoringCommand = new DelegateCommand(OpenSelfSignedCaAuthoring, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked && PrivateKeysPage.HasSelection);
        _openCertificateSigningRequestAuthoringCommand = new DelegateCommand(OpenCertificateSigningRequestAuthoring, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked && PrivateKeysPage.HasSelection);
        _createSelfSignedCaCommand = new AsyncCommand(CreateSelfSignedCaAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked && PrivateKeysPage.HasSelection);
        _createCertificateSigningRequestCommand = new AsyncCommand(CreateCertificateSigningRequestAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked && PrivateKeysPage.HasSelection);
        _exportPrivateKeyCommand = new AsyncCommand(ExportSelectedPrivateKeyAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked && PrivateKeysPage.HasSelection);
        _exportPrivateKeyToFileCommand = new AsyncCommand(ExportSelectedPrivateKeyToFileAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked && PrivateKeysPage.HasSelection);
        _refreshCertificateRequestsCommand = new AsyncCommand(LoadCertificateRequestsAsync, () => !IsBusy && Snapshot.State != DatabaseSessionState.Closed);
        _openIssuanceAuthoringCommand = new DelegateCommand(OpenIssuanceAuthoring, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked && CertificateRequestsPage.HasSelection);
        _signCertificateSigningRequestCommand = new AsyncCommand(SignCertificateSigningRequestAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked && CertificateRequestsPage.HasSelection && CertificateRequestsPage.IssuanceAuthoring.SelectedIssuerCertificate is not null && CertificateRequestsPage.IssuanceAuthoring.SelectedIssuerPrivateKey is not null);
        _exportCertificateSigningRequestCommand = new AsyncCommand(ExportSelectedCertificateSigningRequestAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked && CertificateRequestsPage.HasSelection);
        _exportCertificateSigningRequestToFileCommand = new AsyncCommand(ExportSelectedCertificateSigningRequestToFileAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked && CertificateRequestsPage.HasSelection);
        _navigateRequestPrivateKeyCommand = new DelegateCommand(() => NavigateTo(CertificateRequestsPage.SelectedItem?.PrivateKeyTarget), () => CertificateRequestsPage.SelectedItem?.PrivateKeyTarget is not null);
        _refreshCertificateRevocationListsCommand = new AsyncCommand(LoadCertificateRevocationListsAsync, () => !IsBusy && Snapshot.State != DatabaseSessionState.Closed);
        _exportCertificateRevocationListToFileCommand = new AsyncCommand(ExportSelectedCertificateRevocationListToFileAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked && CertificateRevocationListsPage.HasSelection);
        _navigateCrlIssuerCommand = new DelegateCommand(() => NavigateTo(CertificateRevocationListsPage.Inspector?.IssuerTarget), () => CertificateRevocationListsPage.Inspector?.IssuerTarget is not null);
        _refreshTemplatesCommand = new AsyncCommand(LoadTemplatesAsync, () => !IsBusy && Snapshot.State != DatabaseSessionState.Closed);
        _createTemplateCommand = new AsyncCommand(CreateNewTemplateAsync, () => !IsBusy && Snapshot.State != DatabaseSessionState.Closed);
        _editTemplateCommand = new DelegateCommand(OpenTemplateAuthoringFromSelection, () => !IsBusy && Snapshot.State != DatabaseSessionState.Closed && TemplatesPage.HasSelection);
        _saveTemplateCommand = new AsyncCommand(SaveTemplateAsync, () => !IsBusy && Snapshot.State != DatabaseSessionState.Closed);
        _cloneTemplateCommand = new AsyncCommand(CloneTemplateAsync, () => !IsBusy && Snapshot.State != DatabaseSessionState.Closed && TemplatesPage.HasSelection);
        _toggleTemplateFavoriteCommand = new AsyncCommand(ToggleTemplateFavoriteAsync, () => !IsBusy && Snapshot.State != DatabaseSessionState.Closed && TemplatesPage.HasSelection);
        _toggleTemplateEnabledCommand = new AsyncCommand(ToggleTemplateEnabledAsync, () => !IsBusy && Snapshot.State != DatabaseSessionState.Closed && TemplatesPage.HasSelection);
        _deleteTemplateCommand = new AsyncCommand(DeleteTemplateAsync, () => !IsBusy && Snapshot.State != DatabaseSessionState.Closed && TemplatesPage.HasSelection);
        _applySelfSignedCaTemplateCommand = new AsyncCommand(ApplySelfSignedCaTemplateAsync, () => !IsBusy && Snapshot.State != DatabaseSessionState.Closed && PrivateKeysPage.SelfSignedCaAuthoring.SelectedTemplate is not null);
        _applyCertificateSigningRequestTemplateCommand = new AsyncCommand(ApplyCertificateSigningRequestTemplateAsync, () => !IsBusy && Snapshot.State != DatabaseSessionState.Closed && PrivateKeysPage.CertificateSigningRequestAuthoring.SelectedTemplate is not null);
        _applyIssuanceTemplateCommand = new AsyncCommand(ApplyIssuanceTemplateAsync, () => !IsBusy && Snapshot.State != DatabaseSessionState.Closed && CertificateRequestsPage.IssuanceAuthoring.SelectedTemplate is not null);
        _createTemplateFromCertificateCommand = new DelegateCommand(CreateTemplateFromCertificate, () => CertificatesPage.SelectedItem is not null);
        _createTemplateFromRequestCommand = new DelegateCommand(CreateTemplateFromRequest, () => CertificateRequestsPage.SelectedItem is not null);
        _createSimilarRequestCommand = new DelegateCommand(CreateSimilarRequest, () => CertificateRequestsPage.SelectedItem is not null);
        _closeAuthoringDialogCommand = new DelegateCommand(CloseAuthoringDialog, () => IsAuthoringDialogOpen && !IsBusy);
        _showCertificateDetailsCommand = new DelegateCommand(ShowCertificateDetails, () => CertificatesPage.HasSelection && CertificatesPage.Inspector is not null);
        _closeCertificateDetailCommand = new DelegateCommand(() => CertificatesPage.IsDetailDialogOpen = false);
        _openRevokeDialogCommand = new DelegateCommand(OpenRevokeDialog, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked && CertificatesPage.SelectedItem is { } cert && !string.Equals(cert.RevocationStatus, "Revoked", StringComparison.OrdinalIgnoreCase));
        _closeRevokeDialogCommand = new DelegateCommand(() => CertificatesPage.IsRevokeDialogOpen = false);
        _togglePlainViewCommand = new DelegateCommand(() => CertificatesPage.IsPlainView = !CertificatesPage.IsPlainView);

        _openAboutCommand = new DelegateCommand(() => IsAboutDialogOpen = true);
        _closeAboutCommand = new DelegateCommand(() => IsAboutDialogOpen = false);
        _openOidResolverCommand = new DelegateCommand(() => { OidResolverInput = string.Empty; OidResolverResult = string.Empty; IsOidResolverDialogOpen = true; });
        _closeOidResolverCommand = new DelegateCommand(() => IsOidResolverDialogOpen = false);
        _resolveOidCommand = new DelegateCommand(ResolveOid, () => !string.IsNullOrWhiteSpace(OidResolverInput));
        _openPasswordChangeCommand = new DelegateCommand(() => { PasswordChangeNew = string.Empty; PasswordChangeConfirm = string.Empty; IsPasswordChangeDialogOpen = true; }, () => Snapshot.State == DatabaseSessionState.Unlocked);
        _closePasswordChangeCommand = new DelegateCommand(() => IsPasswordChangeDialogOpen = false);
        _confirmPasswordChangeCommand = new AsyncCommand(ConfirmPasswordChangeAsync, () => !IsBusy && !string.IsNullOrWhiteSpace(PasswordChangeNew) && PasswordChangeNew == PasswordChangeConfirm);
        _pastePemCommand = new AsyncCommand(PastePemAsync, () => !IsBusy && Snapshot.State == DatabaseSessionState.Unlocked);

        SettingsSecurityPage.CreateDatabaseCommand = _createDatabaseCommand;
        SettingsSecurityPage.OpenDatabaseCommand = _openDatabaseCommand;
        SettingsSecurityPage.UnlockDatabaseCommand = _unlockDatabaseCommand;
        SettingsSecurityPage.LockDatabaseCommand = _lockDatabaseCommand;

        CertificatesPage.RefreshCommand = _refreshCertificatesCommand;
        CertificatesPage.ImportFilesCommand = _importFilesCommand;
        CertificatesPage.ImportMaterialCommand = _importMaterialCommand;
        CertificatesPage.ExportSelectedCommand = _exportCertificateToFileCommand;
        CertificatesPage.RevokeSelectedCommand = _revokeCertificateCommand;
        CertificatesPage.GenerateCertificateRevocationListCommand = _generateCertificateRevocationListCommand;
        CertificatesPage.OpenIssuerCommand = _navigateIssuerCommand;
        CertificatesPage.OpenPrivateKeyCommand = _navigatePrivateKeyCommand;
        CertificatesPage.OpenChildCertificateCommand = _navigateChildCertificateCommand;
        CertificatesPage.CreateTemplateFromCertificateCommand = _createTemplateFromCertificateCommand;
        CertificatesPage.ShowDetailsCommand = _showCertificateDetailsCommand;
        CertificatesPage.CloseDetailCommand = _closeCertificateDetailCommand;
        CertificatesPage.OpenRevokeDialogCommand = _openRevokeDialogCommand;
        CertificatesPage.CloseRevokeDialogCommand = _closeRevokeDialogCommand;
        CertificatesPage.TogglePlainViewCommand = _togglePlainViewCommand;

        PrivateKeysPage.RefreshCommand = _refreshPrivateKeysCommand;
        PrivateKeysPage.GenerateKeyCommand = _generateKeyCommand;
        PrivateKeysPage.OpenSelfSignedCaAuthoringCommand = _openSelfSignedCaAuthoringCommand;
        PrivateKeysPage.OpenCertificateSigningRequestAuthoringCommand = _openCertificateSigningRequestAuthoringCommand;
        PrivateKeysPage.CreateSelfSignedCaCommand = _createSelfSignedCaCommand;
        PrivateKeysPage.CreateCertificateSigningRequestCommand = _createCertificateSigningRequestCommand;
        PrivateKeysPage.ApplySelfSignedCaTemplateCommand = _applySelfSignedCaTemplateCommand;
        PrivateKeysPage.ApplyCertificateSigningRequestTemplateCommand = _applyCertificateSigningRequestTemplateCommand;
        PrivateKeysPage.ExportSelectedCommand = _exportPrivateKeyCommand;
        PrivateKeysPage.ExportSelectedToFileCommand = _exportPrivateKeyToFileCommand;
        PrivateKeysPage.SelfSignedCaAuthoring.ApplyTemplateCommand = _applySelfSignedCaTemplateCommand;
        PrivateKeysPage.SelfSignedCaAuthoring.PrimaryActionCommand = _createSelfSignedCaCommand;
        PrivateKeysPage.CertificateSigningRequestAuthoring.ApplyTemplateCommand = _applyCertificateSigningRequestTemplateCommand;
        PrivateKeysPage.CertificateSigningRequestAuthoring.PrimaryActionCommand = _createCertificateSigningRequestCommand;

        CertificateRequestsPage.RefreshCommand = _refreshCertificateRequestsCommand;
        CertificateRequestsPage.OpenIssuanceAuthoringCommand = _openIssuanceAuthoringCommand;
        CertificateRequestsPage.SignSelectedCommand = _signCertificateSigningRequestCommand;
        CertificateRequestsPage.ApplyIssuanceTemplateCommand = _applyIssuanceTemplateCommand;
        CertificateRequestsPage.ExportSelectedCommand = _exportCertificateSigningRequestCommand;
        CertificateRequestsPage.ExportSelectedToFileCommand = _exportCertificateSigningRequestToFileCommand;
        CertificateRequestsPage.OpenSelectedPrivateKeyCommand = _navigateRequestPrivateKeyCommand;
        CertificateRequestsPage.CreateTemplateFromRequestCommand = _createTemplateFromRequestCommand;
        CertificateRequestsPage.CreateSimilarRequestCommand = _createSimilarRequestCommand;
        CertificateRequestsPage.IssuanceAuthoring.ApplyTemplateCommand = _applyIssuanceTemplateCommand;
        CertificateRequestsPage.IssuanceAuthoring.PrimaryActionCommand = _signCertificateSigningRequestCommand;

        CertificateRevocationListsPage.RefreshCommand = _refreshCertificateRevocationListsCommand;
        CertificateRevocationListsPage.ExportSelectedCommand = _exportCertificateRevocationListToFileCommand;
        CertificateRevocationListsPage.OpenIssuerCommand = _navigateCrlIssuerCommand;
        TemplatesPage.RefreshCommand = _refreshTemplatesCommand;
        TemplatesPage.CreateNewCommand = _createTemplateCommand;
        TemplatesPage.EditTemplateCommand = _editTemplateCommand;
        TemplatesPage.SaveTemplateCommand = _saveTemplateCommand;
        TemplatesPage.CloneTemplateCommand = _cloneTemplateCommand;
        TemplatesPage.ToggleFavoriteCommand = _toggleTemplateFavoriteCommand;
        TemplatesPage.ToggleEnabledCommand = _toggleTemplateEnabledCommand;
        TemplatesPage.DeleteTemplateCommand = _deleteTemplateCommand;
        TemplatesPage.Authoring.PrimaryActionCommand = _saveTemplateCommand;

        WorkspaceNavigationItems =
        [
            new NavigationItemViewModel("Private Keys", "Keys", new DelegateCommand(() => SelectPage(PrivateKeysPage))),
            new NavigationItemViewModel("Certificate signing requests", "Requests", new DelegateCommand(() => SelectPage(CertificateRequestsPage))),
            new NavigationItemViewModel("Certificates", "Certificates", new DelegateCommand(() => SelectPage(CertificatesPage))),
            new NavigationItemViewModel("Templates", "Templates", new DelegateCommand(() => SelectPage(TemplatesPage))),
            new NavigationItemViewModel("Revocation lists", "Revocation", new DelegateCommand(() => SelectPage(CertificateRevocationListsPage)))
        ];
        UtilityNavigationItems =
        [
            new NavigationItemViewModel("Dashboard", "Overview", new DelegateCommand(() => SelectPage(DashboardPage))),
            new NavigationItemViewModel("Settings / Security", "Database", new DelegateCommand(() => SelectPage(SettingsSecurityPage)))
        ];
        NavigationItems = [.. WorkspaceNavigationItems, .. UtilityNavigationItems];

        _currentPage = CertificatesPage;

        CertificatesPage.PropertyChanged += OnCertificatesPagePropertyChanged;
        PrivateKeysPage.PropertyChanged += OnPageSelectionChanged;
        CertificateRequestsPage.PropertyChanged += OnPageSelectionChanged;
        CertificateRevocationListsPage.PropertyChanged += OnPageSelectionChanged;
        TemplatesPage.PropertyChanged += OnPageSelectionChanged;
        PrivateKeysPage.SelfSignedCaAuthoring.PropertyChanged += OnAuthoringPropertyChanged;
        PrivateKeysPage.CertificateSigningRequestAuthoring.PropertyChanged += OnAuthoringPropertyChanged;
        CertificateRequestsPage.IssuanceAuthoring.PropertyChanged += OnAuthoringPropertyChanged;
        TemplatesPage.Authoring.PropertyChanged += OnAuthoringPropertyChanged;

        SelectPage(CertificatesPage);
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

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public bool IsAuthoringDialogOpen => AuthoringDialogKind != AuthoringDialogKind.None;

    public AuthoringDialogKind AuthoringDialogKind
    {
        get => _authoringDialogKind;
        private set
        {
            if (SetProperty(ref _authoringDialogKind, value))
            {
                OnPropertyChanged(nameof(IsAuthoringDialogOpen));
                OnPropertyChanged(nameof(IsCertificateAuthoringDialogOpen));
                OnPropertyChanged(nameof(IsTemplateAuthoringDialogOpen));
                RefreshCommandStates();
            }
        }
    }

    public bool IsCertificateAuthoringDialogOpen => AuthoringDialogKind == AuthoringDialogKind.Certificate;

    public bool IsTemplateAuthoringDialogOpen => AuthoringDialogKind == AuthoringDialogKind.Template;

    public CertificateAuthoringViewModel? ActiveCertificateAuthoring
    {
        get => _activeCertificateAuthoring;
        private set => SetProperty(ref _activeCertificateAuthoring, value);
    }

    public string AuthoringDialogTitle
    {
        get => _authoringDialogTitle;
        private set => SetProperty(ref _authoringDialogTitle, value);
    }

    public string AuthoringDialogSubtitle
    {
        get => _authoringDialogSubtitle;
        private set => SetProperty(ref _authoringDialogSubtitle, value);
    }

    public ICommand CloseAuthoringDialogCommand => _closeAuthoringDialogCommand;

    public DashboardPageViewModel DashboardPage { get; }

    public CertificatesPageViewModel CertificatesPage { get; }

    public PrivateKeysPageViewModel PrivateKeysPage { get; }

    public CertificateRequestsPageViewModel CertificateRequestsPage { get; }

    public CertificateRevocationListsPageViewModel CertificateRevocationListsPage { get; }

    public TemplatesPageViewModel TemplatesPage { get; }

    public SettingsSecurityPageViewModel SettingsSecurityPage { get; }

    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }

    public ObservableCollection<NavigationItemViewModel> WorkspaceNavigationItems { get; }

    public ObservableCollection<NavigationItemViewModel> UtilityNavigationItems { get; }

    public ObservableCollection<NotificationItemViewModel> Notifications { get; } = [];

    public PageViewModelBase CurrentPage
    {
        get => _currentPage;
        private set => SetProperty(ref _currentPage, value);
    }

    public DatabaseSessionSnapshot Snapshot => _databaseSessionService.GetSnapshot();

    public string WorkspaceStatus => Snapshot.DisplayName is null
        ? Snapshot.StatusMessage
        : $"{Snapshot.DisplayName} | {Snapshot.StatusMessage}";

    public string CurrentSelectionSummary => CurrentPage switch
    {
        var _ when CurrentPage == PrivateKeysPage => PrivateKeysPage.SelectedItem is null ? "No private key selected" : $"Selected key: {PrivateKeysPage.SelectedItem.DisplayName}",
        var _ when CurrentPage == CertificateRequestsPage => CertificateRequestsPage.SelectedItem is null ? "No request selected" : $"Selected request: {CertificateRequestsPage.SelectedItem.DisplayName}",
        var _ when CurrentPage == CertificatesPage => CertificatesPage.SelectedItem is null ? "No certificate selected" : $"Selected certificate: {CertificatesPage.SelectedItem.DisplayName}",
        var _ when CurrentPage == TemplatesPage => TemplatesPage.SelectedItem is null ? "No template selected" : $"Selected template: {TemplatesPage.SelectedItem.Name}",
        var _ when CurrentPage == CertificateRevocationListsPage => CertificateRevocationListsPage.SelectedItem is null ? "No revocation list selected" : $"Selected CRL: {CertificateRevocationListsPage.SelectedItem.DisplayName}",
        _ => Snapshot.DatabasePath ?? "No workspace selected"
    };

    public ICommand CreateDatabaseCommand => _createDatabaseCommand;

    public ICommand OpenDatabaseCommand => _openDatabaseCommand;

    public ICommand UnlockDatabaseCommand => _unlockDatabaseCommand;

    public ICommand LockDatabaseCommand => _lockDatabaseCommand;

    public ICommand CloseDatabaseCommand => _closeDatabaseCommand;

    public ICommand RefreshWorkspaceCommand => _refreshWorkspaceCommand;

    public ICommand ImportFilesCommand => _importFilesCommand;

    public ICommand ExitCommand => _exitCommand;

    public ICommand DashboardCommand => UtilityNavigationItems[0].Command;

    public ICommand SettingsSecurityCommand => UtilityNavigationItems[1].Command;

    // M15 dialog state properties

    public bool IsAboutDialogOpen
    {
        get => _isAboutDialogOpen;
        set => SetProperty(ref _isAboutDialogOpen, value);
    }

    public bool IsOidResolverDialogOpen
    {
        get => _isOidResolverDialogOpen;
        set => SetProperty(ref _isOidResolverDialogOpen, value);
    }

    public bool IsPasswordChangeDialogOpen
    {
        get => _isPasswordChangeDialogOpen;
        set => SetProperty(ref _isPasswordChangeDialogOpen, value);
    }

    public string OidResolverInput
    {
        get => _oidResolverInput;
        set
        {
            if (SetProperty(ref _oidResolverInput, value))
                _resolveOidCommand.RaiseCanExecuteChanged();
        }
    }

    public string OidResolverResult
    {
        get => _oidResolverResult;
        set => SetProperty(ref _oidResolverResult, value);
    }

    public string PasswordChangeNew
    {
        get => _passwordChangeNew;
        set
        {
            if (SetProperty(ref _passwordChangeNew, value))
                _confirmPasswordChangeCommand.RaiseCanExecuteChanged();
        }
    }

    public string PasswordChangeConfirm
    {
        get => _passwordChangeConfirm;
        set
        {
            if (SetProperty(ref _passwordChangeConfirm, value))
                _confirmPasswordChangeCommand.RaiseCanExecuteChanged();
        }
    }

    // M15 command accessors

    public ICommand OpenAboutCommand => _openAboutCommand;

    public ICommand CloseAboutCommand => _closeAboutCommand;

    public ICommand OpenOidResolverCommand => _openOidResolverCommand;

    public ICommand CloseOidResolverCommand => _closeOidResolverCommand;

    public ICommand ResolveOidCommand => _resolveOidCommand;

    public ICommand OpenPasswordChangeCommand => _openPasswordChangeCommand;

    public ICommand ClosePasswordChangeCommand => _closePasswordChangeCommand;

    public ICommand ConfirmPasswordChangeCommand => _confirmPasswordChangeCommand;

    public ICommand PastePemCommand => _pastePemCommand;

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

    private async Task CloseDatabaseAsync()
    {
        await RunDatabaseActionAsync("Closing database", () => _databaseSessionService.CloseDatabaseAsync(CancellationToken.None));
    }

    private async Task GenerateKeyAsync()
    {
        var algorithm = PrivateKeysPage.SelectedAlgorithm == KeyAlgorithmView.Rsa ? KeyAlgorithmKind.Rsa : KeyAlgorithmKind.Ecdsa;
        var curve = algorithm == KeyAlgorithmKind.Ecdsa
            ? PrivateKeysPage.SelectedCurve == EllipticCurveView.P256 ? EllipticCurveKind.P256 : EllipticCurveKind.P384
            : (EllipticCurveKind?)null;

        using var scope = BeginBusy("Generating managed private key");
        var result = await _databaseSessionService.GenerateStoredKeyAsync(
            new GenerateStoredKeyRequest(PrivateKeysPage.NewKeyDisplayName, algorithm, algorithm == KeyAlgorithmKind.Rsa ? PrivateKeysPage.SelectedKeySize : null, curve),
            CancellationToken.None);

        if (!result.IsSuccess || result.Value is null)
        {
            NotifyFailure(result.Message);
            return;
        }

        PrivateKeysPage.IsNewKeyDialogOpen = false;
        await RefreshAllAsync();
        NavigateTo(new NavigationTarget(BrowserEntityType.PrivateKey, result.Value.PrivateKeyId, NavigationFocusSection.Overview));
        NotifySuccess($"Generated {result.Value.Algorithm} key.");
    }

    private void OpenSelfSignedCaAuthoring()
    {
        if (!PrivateKeysPage.HasSelection || PrivateKeysPage.SelectedItem is null)
        {
            NotifyFailure("Select a private key first.");
            return;
        }

        PrivateKeysPage.SelfSignedCaAuthoring.SourceSummary = $"Source: private key {PrivateKeysPage.SelectedItem.DisplayName}";
        OpenCertificateAuthoringDialog(
            PrivateKeysPage.SelfSignedCaAuthoring,
            "Certificate Input",
            "Self-signed CA authoring");
    }

    private void OpenCertificateSigningRequestAuthoring()
    {
        if (!PrivateKeysPage.HasSelection || PrivateKeysPage.SelectedItem is null)
        {
            NotifyFailure("Select a private key first.");
            return;
        }

        PrivateKeysPage.CertificateSigningRequestAuthoring.SourceSummary = $"Source: private key {PrivateKeysPage.SelectedItem.DisplayName}";
        OpenCertificateAuthoringDialog(
            PrivateKeysPage.CertificateSigningRequestAuthoring,
            "Certificate Input",
            "Certificate request authoring");
    }

    private void OpenIssuanceAuthoring()
    {
        if (!CertificateRequestsPage.HasSelection || CertificateRequestsPage.SelectedItem is null)
        {
            NotifyFailure("Select a CSR first.");
            return;
        }

        CertificateRequestsPage.IssuanceAuthoring.SourceSummary = $"Source: request {CertificateRequestsPage.SelectedItem.DisplayName}";
        OpenCertificateAuthoringDialog(
            CertificateRequestsPage.IssuanceAuthoring,
            "Certificate Input",
            "Issue certificate from selected request");
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
                PrivateKeysPage.SelfSignedCaAuthoring.DisplayName,
                PrivateKeysPage.SelfSignedCaAuthoring.SubjectName,
                Math.Max(1, PrivateKeysPage.SelfSignedCaAuthoring.ValidityDays),
                PrivateKeysPage.SelfSignedCaAuthoring.SelectedTemplate?.TemplateId),
            CancellationToken.None);

        if (!result.IsSuccess || result.Value is null)
        {
            NotifyFailure(result.Message);
            return;
        }

        CloseAuthoringDialog();
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
                PrivateKeysPage.CertificateSigningRequestAuthoring.DisplayName,
                PrivateKeysPage.CertificateSigningRequestAuthoring.SubjectName,
                ParseSubjectAlternativeNames(PrivateKeysPage.CertificateSigningRequestAuthoring.SubjectAlternativeNames),
                PrivateKeysPage.CertificateSigningRequestAuthoring.IsCertificateAuthority,
                PrivateKeysPage.CertificateSigningRequestAuthoring.HasPathLengthConstraint ? PrivateKeysPage.CertificateSigningRequestAuthoring.PathLengthConstraint : null,
                SplitValues(PrivateKeysPage.CertificateSigningRequestAuthoring.KeyUsages),
                SplitValues(PrivateKeysPage.CertificateSigningRequestAuthoring.EnhancedKeyUsages),
                PrivateKeysPage.CertificateSigningRequestAuthoring.SelectedTemplate?.TemplateId),
            CancellationToken.None);

        if (!result.IsSuccess || result.Value is null)
        {
            NotifyFailure(result.Message);
            return;
        }

        CloseAuthoringDialog();
        await RefreshAllAsync();
        NavigateTo(new NavigationTarget(BrowserEntityType.CertificateSigningRequest, result.Value.CertificateSigningRequestId, NavigationFocusSection.Overview));
        NotifySuccess("Certificate signing request created.");
    }

    private async Task SignCertificateSigningRequestAsync()
    {
        if (CertificateRequestsPage.SelectedItem is null
            || CertificateRequestsPage.IssuanceAuthoring.SelectedIssuerCertificate is null
            || CertificateRequestsPage.IssuanceAuthoring.SelectedIssuerPrivateKey is null)
        {
            NotifyFailure("Select a CSR, issuer certificate, and issuer private key.");
            return;
        }

        using var scope = BeginBusy("Signing certificate request");
        var result = await _databaseSessionService.SignCertificateSigningRequestAsync(
            new SignStoredCertificateSigningRequestRequest(
                CertificateRequestsPage.SelectedItem.CertificateSigningRequestId,
                CertificateRequestsPage.IssuanceAuthoring.SelectedIssuerCertificate.CertificateId,
                CertificateRequestsPage.IssuanceAuthoring.SelectedIssuerPrivateKey.PrivateKeyId,
                CertificateRequestsPage.IssuanceAuthoring.DisplayName,
                Math.Max(1, CertificateRequestsPage.IssuanceAuthoring.ValidityDays),
                CertificateRequestsPage.IssuanceAuthoring.SelectedTemplate?.TemplateId),
            CancellationToken.None);

        if (!result.IsSuccess || result.Value is null)
        {
            NotifyFailure(result.Message);
            return;
        }

        CloseAuthoringDialog();
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

        CertificatesPage.IsRevokeDialogOpen = false;
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

    public async Task ImportFilesFromDropAsync(IReadOnlyList<string> filePaths)
    {
        if (filePaths.Count == 0)
        {
            return;
        }

        using var scope = BeginBusy("Importing dropped files");
        await ImportFilesCoreAsync(filePaths);
    }

    private async Task ImportFilesFromPickerAsync()
    {
        var filePaths = await _fileDialogService.PickImportFilesAsync(CancellationToken.None);
        if (filePaths.Count == 0)
        {
            return;
        }

        using var scope = BeginBusy("Importing files");
        await ImportFilesCoreAsync(filePaths);
    }

    private async Task ImportFilesCoreAsync(IReadOnlyList<string> filePaths)
    {
        var result = await _databaseSessionService.ImportStoredFilesAsync(
            new ImportStoredFilesRequest(filePaths, string.IsNullOrWhiteSpace(CertificatesPage.ImportPassword) ? null : CertificatesPage.ImportPassword),
            CancellationToken.None);

        if (!result.IsSuccess || result.Value is null)
        {
            NotifyFailure(result.Message);
            return;
        }

        await RefreshAllAsync();
        var firstImport = result.Value.ImportedFiles.FirstOrDefault();
        if (firstImport is not null)
        {
            if (firstImport.CertificateIds.Count > 0)
            {
                NavigateTo(new NavigationTarget(BrowserEntityType.Certificate, firstImport.CertificateIds[0], NavigationFocusSection.Inspector));
            }
            else if (firstImport.PrivateKeyIds.Count > 0)
            {
                NavigateTo(new NavigationTarget(BrowserEntityType.PrivateKey, firstImport.PrivateKeyIds[0], NavigationFocusSection.Overview));
            }
            else if (firstImport.CertificateSigningRequestIds.Count > 0)
            {
                NavigateTo(new NavigationTarget(BrowserEntityType.CertificateSigningRequest, firstImport.CertificateSigningRequestIds[0], NavigationFocusSection.Overview));
            }
            else if (firstImport.CertificateRevocationListIds.Count > 0)
            {
                NavigateTo(new NavigationTarget(BrowserEntityType.CertificateRevocationList, firstImport.CertificateRevocationListIds[0], NavigationFocusSection.Inspector));
            }
        }

        NotifySuccess($"{result.Value.ImportedFiles.Count} file(s) imported.");
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

    private async Task ExportSelectedCertificateToFileAsync()
    {
        if (!CertificatesPage.HasSelection || CertificatesPage.SelectedItem is null)
        {
            NotifyFailure("Select a certificate first.");
            return;
        }

        var exportFormat = CertificatesPage.SelectedExportTarget == CertificateExportTargetView.Pkcs12Bundle
            ? CryptoDataFormat.Pkcs12
            : MapFormat(CertificatesPage.SelectedExportFormat);
        var exportMode = CertificatesPage.SelectedExportTarget switch
        {
            CertificateExportTargetView.CertificateChain => StoredMaterialExportMode.CertificateChain,
            CertificateExportTargetView.CertificateWithPrivateKeyPem => StoredMaterialExportMode.CertificateWithPrivateKeyBundle,
            CertificateExportTargetView.Pkcs12Bundle => StoredMaterialExportMode.CertificateWithPrivateKeyBundle,
            _ => StoredMaterialExportMode.Default
        };

        var suggestedFileName = BuildSuggestedFileName(
            CertificatesPage.SelectedItem.DisplayName,
            exportFormat,
            exportMode,
            CryptoImportKind.Certificate);
        var destinationPath = await _fileDialogService.PickSavePathAsync(suggestedFileName, CancellationToken.None);
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            return;
        }

        using var scope = BeginBusy("Exporting certificate to file");
        var result = await _databaseSessionService.ExportStoredMaterialToFileAsync(
            new ExportStoredMaterialToFileRequest(
                CryptoImportKind.Certificate,
                CertificatesPage.SelectedItem.CertificateId,
                exportFormat,
                destinationPath,
                string.IsNullOrWhiteSpace(CertificatesPage.SelectedExportPassword) ? null : CertificatesPage.SelectedExportPassword,
                SlugifyFileName(CertificatesPage.SelectedItem.DisplayName),
                exportMode),
            CancellationToken.None);

        if (!result.IsSuccess)
        {
            NotifyFailure(result.Message);
            return;
        }

        NotifySuccess($"Certificate exported to {destinationPath}.");
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

    private async Task ExportSelectedPrivateKeyToFileAsync()
    {
        if (!PrivateKeysPage.HasSelection || PrivateKeysPage.SelectedItem is null)
        {
            NotifyFailure("Select a private key first.");
            return;
        }

        var format = MapFormat(PrivateKeysPage.SelectedExportFormat);
        var destinationPath = await _fileDialogService.PickSavePathAsync(
            BuildSuggestedFileName(PrivateKeysPage.SelectedItem.DisplayName, format, StoredMaterialExportMode.Default, CryptoImportKind.PrivateKey),
            CancellationToken.None);
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            return;
        }

        using var scope = BeginBusy("Exporting private key to file");
        var result = await _databaseSessionService.ExportStoredMaterialToFileAsync(
            new ExportStoredMaterialToFileRequest(
                CryptoImportKind.PrivateKey,
                PrivateKeysPage.SelectedItem.PrivateKeyId,
                format,
                destinationPath,
                string.IsNullOrWhiteSpace(PrivateKeysPage.SelectedExportPassword) ? null : PrivateKeysPage.SelectedExportPassword,
                SlugifyFileName(PrivateKeysPage.SelectedItem.DisplayName)),
            CancellationToken.None);

        if (!result.IsSuccess)
        {
            NotifyFailure(result.Message);
            return;
        }

        NotifySuccess($"Private key exported to {destinationPath}.");
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

    private async Task ExportSelectedCertificateSigningRequestToFileAsync()
    {
        if (!CertificateRequestsPage.HasSelection || CertificateRequestsPage.SelectedItem is null)
        {
            NotifyFailure("Select a CSR first.");
            return;
        }

        var format = MapFormat(CertificateRequestsPage.SelectedExportFormat);
        var destinationPath = await _fileDialogService.PickSavePathAsync(
            BuildSuggestedFileName(CertificateRequestsPage.SelectedItem.DisplayName, format, StoredMaterialExportMode.Default, CryptoImportKind.CertificateSigningRequest),
            CancellationToken.None);
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            return;
        }

        using var scope = BeginBusy("Exporting CSR to file");
        var result = await _databaseSessionService.ExportStoredMaterialToFileAsync(
            new ExportStoredMaterialToFileRequest(
                CryptoImportKind.CertificateSigningRequest,
                CertificateRequestsPage.SelectedItem.CertificateSigningRequestId,
                format,
                destinationPath,
                null,
                SlugifyFileName(CertificateRequestsPage.SelectedItem.DisplayName)),
            CancellationToken.None);

        if (!result.IsSuccess)
        {
            NotifyFailure(result.Message);
            return;
        }

        NotifySuccess($"CSR exported to {destinationPath}.");
    }

    private async Task ExportSelectedCertificateRevocationListToFileAsync()
    {
        if (!CertificateRevocationListsPage.HasSelection || CertificateRevocationListsPage.SelectedItem is null)
        {
            NotifyFailure("Select a CRL first.");
            return;
        }

        var destinationPath = await _fileDialogService.PickSavePathAsync(
            BuildSuggestedFileName(CertificateRevocationListsPage.SelectedItem.DisplayName, CryptoDataFormat.Pem, StoredMaterialExportMode.Default, CryptoImportKind.CertificateRevocationList),
            CancellationToken.None);
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            return;
        }

        using var scope = BeginBusy("Exporting CRL to file");
        var result = await _databaseSessionService.ExportStoredMaterialToFileAsync(
            new ExportStoredMaterialToFileRequest(
                CryptoImportKind.CertificateRevocationList,
                CertificateRevocationListsPage.SelectedItem.CertificateRevocationListId,
                CryptoDataFormat.Pem,
                destinationPath,
                null,
                SlugifyFileName(CertificateRevocationListsPage.SelectedItem.DisplayName)),
            CancellationToken.None);

        if (!result.IsSuccess)
        {
            NotifyFailure(result.Message);
            return;
        }

        NotifySuccess($"CRL exported to {destinationPath}.");
    }

    private async Task RefreshAllAsync()
    {
        using var scope = BeginBusy("Refreshing workspace");
        ApplySnapshot(Snapshot);
        await LoadDiagnosticsAsync();

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
        var certItems = result.IsSuccess && result.Value is not null ? result.Value : [];
        CertificatesPage.SetItems(certItems);
        CertificatesPage.RebuildTree(certItems);
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
            TemplatesPage.SetTemplates([]);
            PrivateKeysPage.SetTemplates([]);
            CertificateRequestsPage.SetTemplates([]);
            return;
        }

        var result = await _databaseSessionService.ListTemplatesAsync(CancellationToken.None);
        var templates = result.IsSuccess && result.Value is not null ? result.Value : [];
        TemplatesPage.SetTemplates(templates);
        PrivateKeysPage.SetTemplates(templates);
        CertificateRequestsPage.SetTemplates(templates);
        await LoadSelectedTemplateAsync();
    }

    private async Task LoadSelectedTemplateAsync()
    {
        if (TemplatesPage.SelectedItem is null || Snapshot.State == DatabaseSessionState.Closed)
        {
            return;
        }

        var result = await _databaseSessionService.GetTemplateAsync(TemplatesPage.SelectedItem.TemplateId, CancellationToken.None);
        if (!result.IsSuccess || result.Value is null)
        {
            NotifyFailure(result.Message);
            return;
        }

        TemplatesPage.LoadTemplate(result.Value);
    }

    private async Task CreateNewTemplateAsync()
    {
        TemplatesPage.PrepareNewTemplate();
        OpenTemplateAuthoringDialog("Template Input", "Create template defaults");
        await Task.CompletedTask;
    }

    private void OpenTemplateAuthoringFromSelection()
    {
        if (!TemplatesPage.HasSelection || TemplatesPage.SelectedItem is null)
        {
            NotifyFailure("Select a template first.");
            return;
        }

        OpenTemplateAuthoringDialog("Template Input", $"Edit template {TemplatesPage.SelectedItem.Name}");
    }

    private async Task SaveTemplateAsync()
    {
        using var scope = BeginBusy("Saving template");
        var result = await _databaseSessionService.SaveTemplateAsync(TemplatesPage.BuildSaveRequest(), CancellationToken.None);
        if (!result.IsSuccess || result.Value is null)
        {
            TemplatesPage.ValidationSummary = result.Message;
            NotifyFailure(result.Message);
            return;
        }

        await LoadTemplatesAsync();
        TemplatesPage.SelectedItem = TemplatesPage.Items.FirstOrDefault(x => x.TemplateId == result.Value.TemplateId);
        TemplatesPage.LoadTemplate(result.Value);
        CloseAuthoringDialog();
        SelectPage(TemplatesPage);
        NotifySuccess(result.Message);
    }

    private async Task CloneTemplateAsync()
    {
        if (TemplatesPage.SelectedItem is null)
        {
            NotifyFailure("Select a template first.");
            return;
        }

        using var scope = BeginBusy("Cloning template");
        var result = await _databaseSessionService.CloneTemplateAsync(new CloneTemplateRequest(TemplatesPage.SelectedItem.TemplateId, null), CancellationToken.None);
        if (!result.IsSuccess || result.Value is null)
        {
            NotifyFailure(result.Message);
            return;
        }

        await LoadTemplatesAsync();
        TemplatesPage.SelectedItem = TemplatesPage.Items.FirstOrDefault(x => x.TemplateId == result.Value.TemplateId);
        TemplatesPage.LoadTemplate(result.Value);
        OpenTemplateAuthoringDialog("Template Input", $"Edit cloned template {result.Value.Name}");
        NotifySuccess("Template cloned.");
    }

    private async Task ToggleTemplateFavoriteAsync()
    {
        if (TemplatesPage.SelectedItem is null)
        {
            NotifyFailure("Select a template first.");
            return;
        }

        using var scope = BeginBusy("Updating template");
        var result = await _databaseSessionService.SetTemplateFavoriteAsync(
            new SetTemplateFavoriteRequest(TemplatesPage.SelectedItem.TemplateId, !TemplatesPage.SelectedItem.IsFavorite),
            CancellationToken.None);
        if (!result.IsSuccess || result.Value is null)
        {
            NotifyFailure(result.Message);
            return;
        }

        await LoadTemplatesAsync();
        TemplatesPage.SelectedItem = TemplatesPage.Items.FirstOrDefault(x => x.TemplateId == result.Value.TemplateId);
        TemplatesPage.LoadTemplate(result.Value);
        NotifySuccess("Template favorite state updated.");
    }

    private async Task ToggleTemplateEnabledAsync()
    {
        if (TemplatesPage.SelectedItem is null)
        {
            NotifyFailure("Select a template first.");
            return;
        }

        using var scope = BeginBusy("Updating template");
        var result = await _databaseSessionService.SetTemplateEnabledAsync(
            new SetTemplateEnabledRequest(TemplatesPage.SelectedItem.TemplateId, !TemplatesPage.SelectedItem.IsEnabled),
            CancellationToken.None);
        if (!result.IsSuccess || result.Value is null)
        {
            NotifyFailure(result.Message);
            return;
        }

        await LoadTemplatesAsync();
        TemplatesPage.SelectedItem = TemplatesPage.Items.FirstOrDefault(x => x.TemplateId == result.Value.TemplateId);
        TemplatesPage.LoadTemplate(result.Value);
        NotifySuccess("Template enabled state updated.");
    }

    private async Task DeleteTemplateAsync()
    {
        if (TemplatesPage.SelectedItem is null)
        {
            NotifyFailure("Select a template first.");
            return;
        }

        using var scope = BeginBusy("Deleting template");
        var result = await _databaseSessionService.DeleteTemplateAsync(TemplatesPage.SelectedItem.TemplateId, CancellationToken.None);
        if (!result.IsSuccess)
        {
            NotifyFailure(result.Message);
            return;
        }

        TemplatesPage.PrepareNewTemplate();
        await LoadTemplatesAsync();
        CloseAuthoringDialog();
        NotifySuccess("Template deleted.");
    }

    private async Task ApplySelfSignedCaTemplateAsync()
    {
        if (PrivateKeysPage.SelfSignedCaAuthoring.SelectedTemplate is null)
        {
            NotifyFailure("Select a template first.");
            return;
        }

        using var scope = BeginBusy("Applying template defaults");
        var result = await _databaseSessionService.ApplyTemplateAsync(
            new ApplyTemplateRequest(PrivateKeysPage.SelfSignedCaAuthoring.SelectedTemplate.TemplateId, TemplateWorkflowKind.SelfSignedCa),
            CancellationToken.None);
        if (!result.IsSuccess || result.Value is null)
        {
            NotifyFailure(result.Message);
            return;
        }

        PrivateKeysPage.SelfSignedCaAuthoring.ApplyTemplateDefaults(result.Value);
        NotifySuccess("Self-signed CA template applied.");
    }

    private async Task ApplyCertificateSigningRequestTemplateAsync()
    {
        if (PrivateKeysPage.CertificateSigningRequestAuthoring.SelectedTemplate is null)
        {
            NotifyFailure("Select a template first.");
            return;
        }

        using var scope = BeginBusy("Applying template defaults");
        var result = await _databaseSessionService.ApplyTemplateAsync(
            new ApplyTemplateRequest(PrivateKeysPage.CertificateSigningRequestAuthoring.SelectedTemplate.TemplateId, TemplateWorkflowKind.CertificateSigningRequest),
            CancellationToken.None);
        if (!result.IsSuccess || result.Value is null)
        {
            NotifyFailure(result.Message);
            return;
        }

        PrivateKeysPage.NewKeyDisplayName = result.Value.DisplayNameDefault;
        PrivateKeysPage.SelectedAlgorithm = result.Value.KeyAlgorithm == KeyAlgorithmKind.Rsa ? KeyAlgorithmView.Rsa : KeyAlgorithmView.Ecdsa;
        PrivateKeysPage.SelectedCurve = result.Value.Curve == EllipticCurveKind.P384 ? EllipticCurveView.P384 : EllipticCurveView.P256;
        PrivateKeysPage.CertificateSigningRequestAuthoring.ApplyTemplateDefaults(result.Value);
        NotifySuccess("CSR template applied.");
    }

    private async Task ApplyIssuanceTemplateAsync()
    {
        if (CertificateRequestsPage.IssuanceAuthoring.SelectedTemplate is null)
        {
            NotifyFailure("Select a template first.");
            return;
        }

        using var scope = BeginBusy("Applying template defaults");
        var result = await _databaseSessionService.ApplyTemplateAsync(
            new ApplyTemplateRequest(CertificateRequestsPage.IssuanceAuthoring.SelectedTemplate.TemplateId, TemplateWorkflowKind.SignCertificateSigningRequest),
            CancellationToken.None);
        if (!result.IsSuccess || result.Value is null)
        {
            NotifyFailure(result.Message);
            return;
        }

        CertificateRequestsPage.IssuanceAuthoring.ApplyTemplateDefaults(result.Value);
        NotifySuccess("Issuance template applied.");
    }

    private void ShowCertificateDetails()
    {
        if (CertificatesPage.SelectedItem is null || CertificatesPage.Inspector is null)
        {
            return;
        }

        CertificatesPage.IsDetailDialogOpen = true;
    }

    private void OpenRevokeDialog()
    {
        if (CertificatesPage.SelectedItem is null)
        {
            return;
        }

        CertificatesPage.SelectedRevocationDate = DateTimeOffset.UtcNow;
        CertificatesPage.IsRevokeDialogOpen = true;
    }

    private void CreateTemplateFromCertificate()
    {
        if (CertificatesPage.SelectedItem is null)
        {
            NotifyFailure("Select a certificate first.");
            return;
        }

        TemplatesPage.PrepareTemplateFromCertificate(CertificatesPage.SelectedItem, CertificatesPage.Inspector);
        OpenTemplateAuthoringDialog("Template Input", $"Derived from certificate {CertificatesPage.SelectedItem.DisplayName}");
        NotifySuccess("Selected certificate copied into the template editor.");
    }

    private void CreateTemplateFromRequest()
    {
        if (CertificateRequestsPage.SelectedItem is null)
        {
            NotifyFailure("Select a CSR first.");
            return;
        }

        TemplatesPage.PrepareTemplateFromCertificateRequest(CertificateRequestsPage.SelectedItem);
        OpenTemplateAuthoringDialog("Template Input", $"Derived from request {CertificateRequestsPage.SelectedItem.DisplayName}");
        NotifySuccess("Selected CSR copied into the template editor.");
    }

    private void CreateSimilarRequest()
    {
        if (CertificateRequestsPage.SelectedItem is null)
        {
            NotifyFailure("Select a CSR first.");
            return;
        }

        PrivateKeysPage.LoadCertificateSigningRequestAuthoringFromRequest(CertificateRequestsPage.SelectedItem);
        if (CertificateRequestsPage.SelectedItem.PrivateKeyId is Guid privateKeyId)
        {
            PrivateKeysPage.SelectedItem = PrivateKeysPage.Items.FirstOrDefault(x => x.PrivateKeyId == privateKeyId) ?? PrivateKeysPage.SelectedItem;
        }

        SelectPage(PrivateKeysPage);
        OpenCertificateAuthoringDialog(
            PrivateKeysPage.CertificateSigningRequestAuthoring,
            "Certificate Input",
            "Create similar request");
        NotifySuccess("CSR values copied into the request authoring surface.");
    }

    private void OpenCertificateAuthoringDialog(CertificateAuthoringViewModel authoring, string title, string subtitle)
    {
        ActiveCertificateAuthoring = authoring;
        AuthoringDialogTitle = title;
        AuthoringDialogSubtitle = subtitle;
        AuthoringDialogKind = AuthoringDialogKind.Certificate;
    }

    private void OpenTemplateAuthoringDialog(string title, string subtitle)
    {
        AuthoringDialogTitle = title;
        AuthoringDialogSubtitle = subtitle;
        ActiveCertificateAuthoring = null;
        AuthoringDialogKind = AuthoringDialogKind.Template;
    }

    private void CloseAuthoringDialog()
    {
        ActiveCertificateAuthoring = null;
        AuthoringDialogTitle = string.Empty;
        AuthoringDialogSubtitle = string.Empty;
        AuthoringDialogKind = AuthoringDialogKind.None;
    }

    private async Task LoadDiagnosticsAsync()
    {
        var result = await _databaseSessionService.GetApplicationDiagnosticsAsync(CancellationToken.None);
        if (!result.IsSuccess || result.Value is null)
        {
            SettingsSecurityPage.ManagedBackendStatus = "Unknown";
            SettingsSecurityPage.OpenSslBackendStatus = "Unknown";
            SettingsSecurityPage.OpenSslVersion = "Unavailable";
            SettingsSecurityPage.OpenSslCapabilities = "Unknown";
            SettingsSecurityPage.RoutingSummary = result.Message;
            return;
        }

        SettingsSecurityPage.ManagedBackendStatus = result.Value.CryptoBackends.ManagedBackendAvailable ? "Available" : "Unavailable";
        SettingsSecurityPage.OpenSslBackendStatus = result.Value.CryptoBackends.OpenSslBackendAvailable
            ? "Available"
            : $"Unavailable{(string.IsNullOrWhiteSpace(result.Value.CryptoBackends.OpenSslLoadError) ? string.Empty : $" ({result.Value.CryptoBackends.OpenSslLoadError})")}";
        SettingsSecurityPage.OpenSslVersion = result.Value.CryptoBackends.OpenSslVersion ?? "Not loaded";
        SettingsSecurityPage.OpenSslCapabilities = result.Value.CryptoBackends.OpenSslCapabilities.Count == 0
            ? "None"
            : string.Join(", ", result.Value.CryptoBackends.OpenSslCapabilities);
        SettingsSecurityPage.RoutingSummary = result.Value.CryptoBackends.RoutingSummary;
        SettingsSecurityPage.AppVersion = result.Value.AppVersion;
        SettingsSecurityPage.SchemaVersion = result.Value.SchemaVersion.ToString();
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
                TemplatesPage.SelectedItem = TemplatesPage.Items.FirstOrDefault(x => x.TemplateId == target.EntityId);
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

        OnPropertyChanged(nameof(CurrentSelectionSummary));
    }

    private void OnCertificatesPagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CertificatesPageViewModel.SelectedItem)
            or nameof(CertificatesPageViewModel.Filter)
            or nameof(CertificatesPageViewModel.IsRevokeDialogOpen)
            or nameof(CertificatesPageViewModel.SelectedChildNavigationItem))
        {
            if (e.PropertyName == nameof(CertificatesPageViewModel.SelectedItem))
            {
                _ = LoadSelectedCertificateInspectorAsync();
            }

            OnPropertyChanged(nameof(CurrentSelectionSummary));
            RefreshCommandStates();
        }
    }

    private void OnPageSelectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PrivateKeysPageViewModel.SelectedItem)
            or nameof(CertificateRequestsPageViewModel.SelectedItem)
            or nameof(TemplatesPageViewModel.SelectedItem)
            or nameof(CertificateRevocationListsPageViewModel.SelectedItem))
        {
            if (sender == PrivateKeysPage && e.PropertyName == nameof(PrivateKeysPageViewModel.SelectedItem))
            {
                var keyLabel = PrivateKeysPage.SelectedItem is null ? "selected private key" : $"private key {PrivateKeysPage.SelectedItem.DisplayName}";
                PrivateKeysPage.SelfSignedCaAuthoring.SourceSummary = $"Source: {keyLabel}";
                PrivateKeysPage.CertificateSigningRequestAuthoring.SourceSummary = $"Source: {keyLabel}";
            }

            if (sender == CertificateRequestsPage && e.PropertyName == nameof(CertificateRequestsPageViewModel.SelectedItem))
            {
                var requestLabel = CertificateRequestsPage.SelectedItem is null ? "selected certificate request" : $"request {CertificateRequestsPage.SelectedItem.DisplayName}";
                CertificateRequestsPage.IssuanceAuthoring.SourceSummary = $"Source: {requestLabel}";
            }

            if (sender == CertificateRevocationListsPage && e.PropertyName == nameof(CertificateRevocationListsPageViewModel.SelectedItem))
            {
                _ = LoadSelectedCertificateRevocationListInspectorAsync();
            }

            if (sender == TemplatesPage && e.PropertyName == nameof(TemplatesPageViewModel.SelectedItem))
            {
                _ = LoadSelectedTemplateAsync();
            }

            OnPropertyChanged(nameof(CurrentSelectionSummary));
            RefreshCommandStates();
        }
    }

    private void OnAuthoringPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CertificateAuthoringViewModel.SelectedTemplate)
            or nameof(CertificateAuthoringViewModel.SelectedIssuerCertificate)
            or nameof(CertificateAuthoringViewModel.SelectedIssuerPrivateKey))
        {
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

        OnPropertyChanged(nameof(WorkspaceStatus));
        OnPropertyChanged(nameof(CurrentSelectionSummary));
        RefreshCommandStates();
    }

    private void ClearBrowseState()
    {
        CloseAuthoringDialog();
        DashboardPage.CertificateCount = 0;
        DashboardPage.PrivateKeyCount = 0;
        DashboardPage.CertificateRequestCount = 0;
        DashboardPage.CertificateRevocationListCount = 0;
        DashboardPage.TemplateCount = 0;
        CertificatesPage.SetItems([]);
        CertificatesPage.RebuildTree([]);
        CertificatesPage.Inspector = null;
        CertificatesPage.SelectedChildNavigationItem = null;
        CertificatesPage.IsDetailDialogOpen = false;
        CertificatesPage.IsRevokeDialogOpen = false;
        PrivateKeysPage.SetItems([]);
        CertificateRequestsPage.SetItems([]);
        CertificateRequestsPage.SetIssuers([], []);
        CertificateRevocationListsPage.SetItems([]);
        CertificateRevocationListsPage.Inspector = null;
        TemplatesPage.SetTemplates([]);
        TemplatesPage.PrepareNewTemplate();
        PrivateKeysPage.SetTemplates([]);
        CertificateRequestsPage.SetTemplates([]);
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
            && CertificatesPage.IsRevokeDialogOpen;
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

    private void ResolveOid()
    {
        if (string.IsNullOrWhiteSpace(_oidResolverInput))
        {
            OidResolverResult = string.Empty;
            return;
        }

        try
        {
            var oid = new System.Security.Cryptography.Oid(_oidResolverInput.Trim());
            if (oid.Value is null && oid.FriendlyName is null)
            {
                OidResolverResult = "OID not found.";
            }
            else
            {
                OidResolverResult = $"OID: {oid.Value ?? "(unknown)"}\nName: {oid.FriendlyName ?? "(unknown)"}";
            }
        }
        catch
        {
            OidResolverResult = "Invalid OID or name.";
        }
    }

    private async Task ConfirmPasswordChangeAsync()
    {
        if (string.IsNullOrWhiteSpace(PasswordChangeNew) || PasswordChangeNew != PasswordChangeConfirm)
        {
            NotifyFailure("Passwords do not match.");
            return;
        }

        using var scope = BeginBusy("Changing password");
        var result = await _databaseSessionService.ChangePasswordAsync(PasswordChangeNew, CancellationToken.None);
        IsPasswordChangeDialogOpen = false;
        if (!result.IsSuccess)
        {
            NotifyFailure(result.Message);
            return;
        }

        NotifySuccess("Password changed.");
    }

    private async Task PastePemAsync()
    {
        var text = await _fileDialogService.GetClipboardTextAsync(CancellationToken.None);
        if (string.IsNullOrWhiteSpace(text))
        {
            NotifyFailure("Clipboard is empty or does not contain text.");
            return;
        }

        CertificatesPage.ImportPayload = text;
        CertificatesPage.ImportDisplayName = "Pasted PEM";
        SelectPage(CertificatesPage);
        NotifySuccess("PEM content pasted — review and confirm in the Import dialog.");
    }

    private void RefreshCommandStates()
    {
        _createDatabaseCommand.RaiseCanExecuteChanged();
        _openDatabaseCommand.RaiseCanExecuteChanged();
        _unlockDatabaseCommand.RaiseCanExecuteChanged();
        _lockDatabaseCommand.RaiseCanExecuteChanged();
        _closeDatabaseCommand.RaiseCanExecuteChanged();
        _refreshWorkspaceCommand.RaiseCanExecuteChanged();
        _refreshCertificatesCommand.RaiseCanExecuteChanged();
        _importFilesCommand.RaiseCanExecuteChanged();
        _importMaterialCommand.RaiseCanExecuteChanged();
        _exportCertificateCommand.RaiseCanExecuteChanged();
        _exportCertificateToFileCommand.RaiseCanExecuteChanged();
        _revokeCertificateCommand.RaiseCanExecuteChanged();
        _generateCertificateRevocationListCommand.RaiseCanExecuteChanged();
        _navigateIssuerCommand.RaiseCanExecuteChanged();
        _navigatePrivateKeyCommand.RaiseCanExecuteChanged();
        _navigateChildCertificateCommand.RaiseCanExecuteChanged();
        _refreshPrivateKeysCommand.RaiseCanExecuteChanged();
        _generateKeyCommand.RaiseCanExecuteChanged();
        _openSelfSignedCaAuthoringCommand.RaiseCanExecuteChanged();
        _openCertificateSigningRequestAuthoringCommand.RaiseCanExecuteChanged();
        _createSelfSignedCaCommand.RaiseCanExecuteChanged();
        _createCertificateSigningRequestCommand.RaiseCanExecuteChanged();
        _exportPrivateKeyCommand.RaiseCanExecuteChanged();
        _exportPrivateKeyToFileCommand.RaiseCanExecuteChanged();
        _refreshCertificateRequestsCommand.RaiseCanExecuteChanged();
        _openIssuanceAuthoringCommand.RaiseCanExecuteChanged();
        _signCertificateSigningRequestCommand.RaiseCanExecuteChanged();
        _exportCertificateSigningRequestCommand.RaiseCanExecuteChanged();
        _exportCertificateSigningRequestToFileCommand.RaiseCanExecuteChanged();
        _navigateRequestPrivateKeyCommand.RaiseCanExecuteChanged();
        _createTemplateFromCertificateCommand.RaiseCanExecuteChanged();
        _createTemplateFromRequestCommand.RaiseCanExecuteChanged();
        _createSimilarRequestCommand.RaiseCanExecuteChanged();
        _refreshCertificateRevocationListsCommand.RaiseCanExecuteChanged();
        _exportCertificateRevocationListToFileCommand.RaiseCanExecuteChanged();
        _navigateCrlIssuerCommand.RaiseCanExecuteChanged();
        _refreshTemplatesCommand.RaiseCanExecuteChanged();
        _createTemplateCommand.RaiseCanExecuteChanged();
        _editTemplateCommand.RaiseCanExecuteChanged();
        _saveTemplateCommand.RaiseCanExecuteChanged();
        _cloneTemplateCommand.RaiseCanExecuteChanged();
        _toggleTemplateFavoriteCommand.RaiseCanExecuteChanged();
        _toggleTemplateEnabledCommand.RaiseCanExecuteChanged();
        _deleteTemplateCommand.RaiseCanExecuteChanged();
        _applySelfSignedCaTemplateCommand.RaiseCanExecuteChanged();
        _applyCertificateSigningRequestTemplateCommand.RaiseCanExecuteChanged();
        _applyIssuanceTemplateCommand.RaiseCanExecuteChanged();
        _closeAuthoringDialogCommand.RaiseCanExecuteChanged();
        _showCertificateDetailsCommand.RaiseCanExecuteChanged();
        _closeCertificateDetailCommand.RaiseCanExecuteChanged();
        _openRevokeDialogCommand.RaiseCanExecuteChanged();
        _closeRevokeDialogCommand.RaiseCanExecuteChanged();
        _togglePlainViewCommand.RaiseCanExecuteChanged();
        _openPasswordChangeCommand.RaiseCanExecuteChanged();
        _confirmPasswordChangeCommand.RaiseCanExecuteChanged();
        _pastePemCommand.RaiseCanExecuteChanged();
    }

    private static IReadOnlyList<SanEntry> ParseSubjectAlternativeNames(string value)
    {
        return value
            .Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => new SanEntry(x))
            .ToList();
    }

    private static IReadOnlyList<string> SplitValues(string value)
    {
        return value
            .Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
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

    private static string BuildSuggestedFileName(string displayName, CryptoDataFormat format, StoredMaterialExportMode mode, CryptoImportKind kind)
    {
        var baseName = SlugifyFileName(displayName);
        return kind switch
        {
            CryptoImportKind.PrivateKey => format == CryptoDataFormat.Pem ? $"{baseName}.key.pem" : $"{baseName}.key",
            CryptoImportKind.CertificateSigningRequest => format == CryptoDataFormat.Pem ? $"{baseName}.csr.pem" : $"{baseName}.csr",
            CryptoImportKind.CertificateRevocationList => format == CryptoDataFormat.Der ? $"{baseName}.crl" : $"{baseName}.crl.pem",
            CryptoImportKind.Certificate when format == CryptoDataFormat.Pkcs12 => $"{baseName}.pfx",
            CryptoImportKind.Certificate when mode == StoredMaterialExportMode.CertificateChain => $"{baseName}-chain.pem",
            CryptoImportKind.Certificate when mode == StoredMaterialExportMode.CertificateWithPrivateKeyBundle => $"{baseName}-bundle.pem",
            CryptoImportKind.Certificate when format == CryptoDataFormat.Der => $"{baseName}.cer",
            _ => $"{baseName}.pem"
        };
    }

    private static string SlugifyFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "xcanet-export" : sanitized;
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
