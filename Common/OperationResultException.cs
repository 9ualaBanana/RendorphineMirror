namespace Common;

public class OperationResultException : Exception
{
    public IOperationError Error { get; }

    public OperationResultException(IOperationError error) : this(error, error.ToString()) { }
    public OperationResultException(IOperationError error, string? message) : this(error, message, null) { }
    public OperationResultException(IOperationError error, string? message, Exception? innerException) : base(message, innerException) => Error = error;
}
