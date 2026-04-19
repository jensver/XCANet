namespace XcaNet.Contracts.Browser;

public sealed record CertificateInspectorData(
    Guid CertificateId,
    CertificateDisplayFields Display,
    CertificateRawFields Raw,
    CertificateExtensionFields Extensions,
    CertificateRevocationInfo Revocation,
    CertificateNavigationInfo Navigation);
