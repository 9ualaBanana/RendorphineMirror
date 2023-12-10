namespace Node.Common;

public class NodeProcessException : Exception
{
    public required int ExitCode { get; init; }

    public NodeProcessException(string? message) : base(message) { }
    public NodeProcessException(string? message, Exception? innerException) : base(message, innerException) { }
}
