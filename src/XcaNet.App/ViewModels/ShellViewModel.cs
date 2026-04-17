using System.ComponentModel;
using System.Runtime.CompilerServices;
using XcaNet.App.Commands;
using XcaNet.Application.Services;
using XcaNet.Contracts.Database;
using XcaNet.Contracts.Results;
using Microsoft.Extensions.Logging;

namespace XcaNet.App.ViewModels;

public sealed class ShellViewModel : INotifyPropertyChanged
{
    private readonly IDatabaseSessionService _databaseSessionService;
    private readonly ILogger<ShellViewModel> _logger;
    private string _databasePath;
    private string _password;
    private string _displayName;
    private string _statusMessage;
    private string _subtitle;

    public ShellViewModel(IDatabaseSessionService databaseSessionService, ILogger<ShellViewModel> logger)
    {
        _databaseSessionService = databaseSessionService;
        _logger = logger;

        logger.LogInformation("Initializing XcaNet shell.");

        Title = "XcaNet";
        _subtitle = "Milestone 2 storage and security";
        _databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "XcaNet", "xcanet.db");
        _displayName = "Primary XcaNet Database";
        _password = string.Empty;
        _statusMessage = _databaseSessionService.GetSnapshot().StatusMessage;

        CreateDatabaseCommand = new AsyncCommand(CreateDatabaseAsync);
        OpenDatabaseCommand = new AsyncCommand(OpenDatabaseAsync);
        UnlockDatabaseCommand = new AsyncCommand(UnlockDatabaseAsync);
        LockDatabaseCommand = new AsyncCommand(LockDatabaseAsync);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title { get; }

    public string Subtitle
    {
        get => _subtitle;
        private set => SetProperty(ref _subtitle, value);
    }

    public string DatabasePath
    {
        get => _databasePath;
        set => SetProperty(ref _databasePath, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public DatabaseSessionSnapshot Snapshot => _databaseSessionService.GetSnapshot();

    public AsyncCommand CreateDatabaseCommand { get; }

    public AsyncCommand OpenDatabaseCommand { get; }

    public AsyncCommand UnlockDatabaseCommand { get; }

    public AsyncCommand LockDatabaseCommand { get; }

    private async Task CreateDatabaseAsync()
    {
        var result = await _databaseSessionService.CreateDatabaseAsync(
            new CreateDatabaseRequest(DatabasePath, Password, DisplayName),
            CancellationToken.None);

        ApplyResult(result);
    }

    private async Task OpenDatabaseAsync()
    {
        var result = await _databaseSessionService.OpenDatabaseAsync(
            new OpenDatabaseRequest(DatabasePath),
            CancellationToken.None);

        ApplyResult(result);
    }

    private async Task UnlockDatabaseAsync()
    {
        var result = await _databaseSessionService.UnlockDatabaseAsync(
            new UnlockDatabaseRequest(Password),
            CancellationToken.None);

        ApplyResult(result);
    }

    private async Task LockDatabaseAsync()
    {
        var result = await _databaseSessionService.LockDatabaseAsync(CancellationToken.None);
        ApplyResult(result);
    }

    private void ApplyResult(OperationResult<DatabaseSessionSnapshot> result)
    {
        Subtitle = Snapshot.State switch
        {
            DatabaseSessionState.Unlocked => "Database unlocked",
            DatabaseSessionState.Locked => "Database open",
            _ => "Milestone 2 storage and security"
        };

        StatusMessage = result.Message;
        _logger.LogInformation("Database UI action completed with state {State}.", Snapshot.State);

        OnPropertyChanged(nameof(Snapshot));
    }

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
