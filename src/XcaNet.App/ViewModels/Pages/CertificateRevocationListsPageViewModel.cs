using System.Collections.ObjectModel;
using System.Windows.Input;
using XcaNet.Contracts.Browser;

namespace XcaNet.App.ViewModels.Pages;

public sealed class CertificateRevocationListsPageViewModel : PageViewModelBase
{
    public CertificateRevocationListsPageViewModel()
        : base("CRLs")
    {
    }

    public ObservableCollection<CertificateRevocationListItem> Items { get; } = [];

    public string PlaceholderMessage => "CRL generation is deferred. Existing CRL records appear here when present.";

    public ICommand? RefreshCommand { get; set; }

    public void SetItems(IEnumerable<CertificateRevocationListItem> items)
    {
        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(item);
        }
    }
}
