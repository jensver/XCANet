namespace XcaNet.App.ViewModels.Notifications;

public sealed class NotificationItemViewModel : ViewModelBase
{
    public NotificationItemViewModel(string level, string message)
    {
        Level = level;
        Message = message;
        Timestamp = DateTimeOffset.UtcNow;
    }

    public string Level { get; }

    public string Message { get; }

    public DateTimeOffset Timestamp { get; }

    public bool IsSuccess => string.Equals(Level, "Success", StringComparison.OrdinalIgnoreCase);

    public bool IsWarning => string.Equals(Level, "Warning", StringComparison.OrdinalIgnoreCase);

    public bool IsError => string.Equals(Level, "Error", StringComparison.OrdinalIgnoreCase);

    public bool IsInfo => !IsSuccess && !IsWarning && !IsError;
}
