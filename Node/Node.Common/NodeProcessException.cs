using System.Runtime.Serialization;

namespace Node.Common;

public class NodeProcessException : Exception
{
    public required int ExitCode { get; init; }

    public NodeProcessException(string? message) : base(message) { }
    public NodeProcessException(string? message, Exception? innerException) : base(message, innerException) { }
    protected NodeProcessException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
