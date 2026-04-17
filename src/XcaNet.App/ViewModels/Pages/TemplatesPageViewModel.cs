using System.Collections.ObjectModel;
using System.Windows.Input;
using XcaNet.Contracts.Browser;

namespace XcaNet.App.ViewModels.Pages;

public sealed class TemplatesPageViewModel : PageViewModelBase
{
    public TemplatesPageViewModel()
        : base("Templates")
    {
    }

    public ObservableCollection<TemplateListItem> Items { get; } = [];

    public string PlaceholderMessage => "Advanced template editing is deferred. Saved template stubs will appear here.";

    public ICommand? RefreshCommand { get; set; }

    public void SetItems(IEnumerable<TemplateListItem> items)
    {
        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(item);
        }
    }
}
