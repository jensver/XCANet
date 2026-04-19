namespace XcaNet.App.ViewModels.Pages;

public abstract class SelectableItemsPageViewModelBase<TItem, TId> : ItemsPageViewModelBase<TItem>
    where TItem : class
    where TId : struct
{
    private TItem? _selectedItem;

    protected SelectableItemsPageViewModelBase(string title)
        : base(title)
    {
    }

    public TItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                OnPropertyChanged(nameof(HasSelection));
            }
        }
    }

    public bool HasSelection => SelectedItem is not null;

    protected abstract TId GetItemId(TItem item);

    protected override void OnItemsChanged()
    {
        TId? previousId = SelectedItem is null ? null : GetItemId(SelectedItem);
        SelectedItem = previousId is null
            ? Items.FirstOrDefault()
            : Items.FirstOrDefault(x => EqualityComparer<TId>.Default.Equals(GetItemId(x), previousId.Value)) ?? Items.FirstOrDefault();
    }
}
