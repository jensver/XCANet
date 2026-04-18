using XcaNet.Contracts.Browser;

namespace XcaNet.App.ViewModels.Pages;

public enum TemplateUsageFilterView
{
    All = 0,
    SelfSignedCa = 1,
    IntermediateCa = 2,
    EndEntityCertificate = 3,
    CertificateSigningRequest = 4
}
