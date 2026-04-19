namespace XcaNet.Contracts.Results;

public sealed class OperationResult<T> : OperationResult
{
    private OperationResult(bool isSuccess, T? value, OperationErrorCode errorCode, string message, IReadOnlyList<string>? warnings = null)
        : base(isSuccess, errorCode, message, warnings)
    {
        Value = value;
    }

    public T? Value { get; }

    public static OperationResult<T> Success(T value, string message, IReadOnlyList<string>? warnings = null)
        => new(true, value, OperationErrorCode.None, message, warnings);

    public static new OperationResult<T> Failure(OperationErrorCode errorCode, string message, IReadOnlyList<string>? warnings = null)
        => new(false, default, errorCode, message, warnings);
}
