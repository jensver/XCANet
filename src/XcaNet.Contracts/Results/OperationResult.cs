namespace XcaNet.Contracts.Results;

public class OperationResult
{
    protected OperationResult(bool isSuccess, OperationErrorCode errorCode, string message, IReadOnlyList<string>? warnings = null)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        Message = message;
        Warnings = warnings ?? [];
    }

    public bool IsSuccess { get; }

    public OperationErrorCode ErrorCode { get; }

    public string Message { get; }

    public IReadOnlyList<string> Warnings { get; }

    public static OperationResult Success(string message, IReadOnlyList<string>? warnings = null)
        => new(true, OperationErrorCode.None, message, warnings);

    public static OperationResult Failure(OperationErrorCode errorCode, string message, IReadOnlyList<string>? warnings = null)
        => new(false, errorCode, message, warnings);
}
