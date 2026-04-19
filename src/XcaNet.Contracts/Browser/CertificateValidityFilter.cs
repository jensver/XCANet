namespace XcaNet.Contracts.Browser;

public enum CertificateValidityFilter
{
    All = 0,
    Valid = 1,
    ExpiringSoon = 2,
    Expired = 3,
    Revoked = 4
}
