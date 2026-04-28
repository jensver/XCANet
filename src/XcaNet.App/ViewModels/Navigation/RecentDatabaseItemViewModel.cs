using System.Windows.Input;

namespace XcaNet.App.ViewModels.Navigation;

public sealed class RecentDatabaseItemViewModel
{
    public RecentDatabaseItemViewModel(string path, ICommand command)
    {
        Path = path;
        Header = path.Length > 60 ? $"...{path[^57..]}" : path;
        Command = command;
    }

    public string Header { get; }

    public string Path { get; }

    public ICommand Command { get; }
}
