namespace XcaNet.Contracts.Results;

public enum OperationErrorCode
{
    None = 0,
    ValidationFailed = 1,
    DatabaseAlreadyExists = 2,
    DatabaseNotFound = 3,
    DatabaseNotOpen = 4,
    DatabaseLocked = 5,
    InvalidPassword = 6,
    MigrationFailed = 7,
    StorageFailure = 8,
    Conflict = 9
}
