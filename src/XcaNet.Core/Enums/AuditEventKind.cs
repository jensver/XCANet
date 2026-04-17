namespace XcaNet.Core.Enums;

public static class AuditEventKind
{
    public const string DatabaseCreated = "database_created";
    public const string DatabaseOpened = "database_opened";
    public const string DatabaseUnlocked = "database_unlocked";
    public const string DatabaseLocked = "database_locked";
    public const string PrivateKeyImported = "private_key_imported";
    public const string PrivateKeyStored = "private_key_stored";
}
