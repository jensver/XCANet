using System.Collections.ObjectModel;
using System.Windows.Input;

namespace XcaNet.App.ViewModels.Pages;

public abstract class ItemsPageViewModelBase<TItem> : PageViewModelBase
{
    protected ItemsPageViewModelBase(string title)
        : base(title)
    {
    }

    public ObservableCollection<TItem> Items { get; } = [];

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
    }
}
