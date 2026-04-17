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
}
