namespace XcaNet.App.ViewModels.Pages;

public sealed class DashboardPageViewModel : PageViewModelBase
{
    private int _certificateCount;
    private int _privateKeyCount;
    private int _certificateRequestCount;
    private int _certificateRevocationListCount;
    private int _templateCount;
    private string _sessionState = "No database is open.";
    private string _databaseDisplayName = "No database selected";
    private string _databasePath = "Open or create a database to begin.";

    public DashboardPageViewModel()
        : base("Dashboard")
    {
    }

    public int CertificateCount
    {
        get => _certificateCount;
        set => SetProperty(ref _certificateCount, value);
    }

    public int PrivateKeyCount
    {
        get => _privateKeyCount;
        set => SetProperty(ref _privateKeyCount, value);
    }

    public int CertificateRequestCount
    {
        get => _certificateRequestCount;
        set => SetProperty(ref _certificateRequestCount, value);
    }

    public int CertificateRevocationListCount
    {
        get => _certificateRevocationListCount;
        set => SetProperty(ref _certificateRevocationListCount, value);
    }

    public int TemplateCount
    {
        get => _templateCount;
        set => SetProperty(ref _templateCount, value);
    }

    public string SessionState
    {
        get => _sessionState;
        set => SetProperty(ref _sessionState, value);
    }

    public string DatabaseDisplayName
    {
        get => _databaseDisplayName;
        set => SetProperty(ref _databaseDisplayName, value);
    }

    public string DatabasePath
    {
        get => _databasePath;
        set => SetProperty(ref _databasePath, value);
    }
}
