namespace XcaNet.Core.Enums;

public static class AuditEventKind
{
    public const string DatabaseCreated = "database_created";
    public const string DatabaseOpened = "database_opened";
    public const string DatabaseUnlocked = "database_unlocked";
    public const string DatabaseLocked = "database_locked";
    public const string PrivateKeyGenerated = "private_key_generated";
    public const string PrivateKeyImported = "private_key_imported";
    public const string PrivateKeyStored = "private_key_stored";
    public const string PrivateKeyExported = "private_key_exported";
    public const string CertificateCreated = "certificate_created";
    public const string CertificateRevoked = "certificate_revoked";
    public const string CertificateImported = "certificate_imported";
    public const string CertificateExported = "certificate_exported";
    public const string CertificateRevocationListGenerated = "crl_generated";
    public const string CertificateSigningRequestCreated = "csr_created";
    public const string CertificateSigningRequestSigned = "csr_signed";
}
