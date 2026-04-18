using System.Collections.ObjectModel;
using System.Windows.Input;
using XcaNet.Contracts.Browser;

namespace XcaNet.App.ViewModels.Pages;

public sealed class TemplatesPageViewModel : ItemsPageViewModelBase<TemplateListItem>
{
    public TemplatesPageViewModel()
        : base("Templates")
    {
        EmptyStateTitle = "No templates saved";
        EmptyStateMessage = "Template authoring is deferred, but saved template stubs will appear here when they are available.";
    }

    public string PlaceholderMessage => "Advanced template editing is deferred. Saved template stubs will appear here.";
}
