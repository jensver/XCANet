using System.Windows.Input;
using XcaNet.Contracts.Database;

namespace XcaNet.App.ViewModels.Pages;

public sealed class SettingsSecurityPageViewModel : PageViewModelBase
{
    private string _databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "XcaNet", "xcanet.db");
    private string _displayName = "Primary XcaNet Database";
    private string _password = string.Empty;
    private string _statusMessage = "No database is open.";
    private string _sessionState = DatabaseSessionState.Closed.ToString();
    private string _lastOpened = "Never";
    private string _managedBackendStatus = "Available";
    private string _openSslBackendStatus = "Unavailable";
    private string _openSslVersion = "Not loaded";
    private string _openSslCapabilities = "None";
    private string _routingSummary = "Managed remains the default backend.";
    private string _appVersion = "Unknown";
    private string _schemaVersion = "1";

    public SettingsSecurityPageViewModel()
        : base("Settings / Security")
    {
    }

    public string DatabasePath
    {
        get => _databasePath;
        set => SetProperty(ref _databasePath, value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string SessionState
    {
        get => _sessionState;
        set => SetProperty(ref _sessionState, value);
    }

    public string LastOpened
    {
        get => _lastOpened;
        set => SetProperty(ref _lastOpened, value);
    }

    public string ManagedBackendStatus
    {
        get => _managedBackendStatus;
        set => SetProperty(ref _managedBackendStatus, value);
    }

    public string OpenSslBackendStatus
    {
        get => _openSslBackendStatus;
        set => SetProperty(ref _openSslBackendStatus, value);
    }

    public string OpenSslVersion
    {
        get => _openSslVersion;
        set => SetProperty(ref _openSslVersion, value);
    }

    public string OpenSslCapabilities
    {
        get => _openSslCapabilities;
        set => SetProperty(ref _openSslCapabilities, value);
    }

    public string RoutingSummary
    {
        get => _routingSummary;
        set => SetProperty(ref _routingSummary, value);
    }

    public string AppVersion
    {
        get => _appVersion;
        set => SetProperty(ref _appVersion, value);
    }

    public string SchemaVersion
    {
        get => _schemaVersion;
        set => SetProperty(ref _schemaVersion, value);
    }

    public ICommand? CreateDatabaseCommand { get; set; }

    public ICommand? OpenDatabaseCommand { get; set; }

    public ICommand? UnlockDatabaseCommand { get; set; }

    public ICommand? LockDatabaseCommand { get; set; }
}
