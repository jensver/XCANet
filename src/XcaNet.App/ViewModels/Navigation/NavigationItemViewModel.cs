using System.Windows.Input;

namespace XcaNet.App.ViewModels.Navigation;

public sealed class NavigationItemViewModel : ViewModelBase
{
    private bool _isSelected;

    public NavigationItemViewModel(string title, string glyph, ICommand command)
    {
        Title = title;
        Glyph = glyph;
        Command = command;
    }

    public string Title { get; }

    public string Glyph { get; }

    public ICommand Command { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
