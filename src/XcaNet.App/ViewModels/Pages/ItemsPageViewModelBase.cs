using System.Collections.ObjectModel;
using System.Windows.Input;

namespace XcaNet.App.ViewModels.Pages;

public abstract class ItemsPageViewModelBase<TItem> : PageViewModelBase
{
    private string _emptyStateTitle;
    private string _emptyStateMessage;

    protected ItemsPageViewModelBase(string title)
        : base(title)
    {
        _emptyStateTitle = $"No {title.ToLowerInvariant()} available";
        _emptyStateMessage = "Add or import content to get started.";
    }

    public ObservableCollection<TItem> Items { get; } = [];

    public bool HasItems => Items.Count > 0;

    public bool IsEmpty => Items.Count == 0;

    public string EmptyStateTitle
    {
        get => _emptyStateTitle;
        protected set => SetProperty(ref _emptyStateTitle, value);
    }

    public string EmptyStateMessage
    {
        get => _emptyStateMessage;
        protected set => SetProperty(ref _emptyStateMessage, value);
    }

    public ICommand? RefreshCommand { get; set; }

    public void SetItems(IEnumerable<TItem> items)
    {
        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(item);
        }

        OnItemsChanged();
    }

    protected virtual void OnItemsChanged()
    {
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(IsEmpty));
    }
}
