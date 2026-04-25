using System.Collections.ObjectModel;
using XcaNet.Contracts.Browser;

namespace XcaNet.App.ViewModels.Pages;

public sealed class CertificateTreeNodeViewModel
{
    public CertificateTreeNodeViewModel(CertificateListItem item)
    {
        Item = item;
        Children = [];
    }

    public CertificateListItem Item { get; }

    public Guid CertificateId => Item.CertificateId;

    public string DisplayName => Item.DisplayName;

    public string CommonName => Item.CommonName;

    public string StatusDisplay => Item.StatusDisplay;

    public string NotBeforeDisplay => Item.NotBefore?.ToString("yyyy-MM-dd") ?? string.Empty;

    public string NotAfterDisplay => Item.NotAfter?.ToString("yyyy-MM-dd") ?? string.Empty;

    public string SerialNumber => Item.SerialNumber;

    public string Sha1Thumbprint => Item.Sha1Thumbprint;

    public bool IsCertificateAuthority => Item.IsCertificateAuthority;

    public ObservableCollection<CertificateTreeNodeViewModel> Children { get; }
}
