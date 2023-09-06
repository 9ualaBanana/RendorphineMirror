namespace Node.Common;

public class TaskFailedException : Exception
{
    public string? FullError { get; init; }

    public TaskFailedException(string message) : base(message) { }
    public TaskFailedException(string message, Exception innerException) : base(message, innerException) { }
}