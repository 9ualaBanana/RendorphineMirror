using System.Runtime.Serialization;

namespace Node.Common;

public class NodeProcessException : Exception
{
    public int ExitCode { get; }

    public NodeProcessException(int exitcode) => ExitCode = exitcode;
    public NodeProcessException(int exitcode, string? message) : base(message) => ExitCode = exitcode;
    public NodeProcessException(int exitcode, string? message, Exception? innerException) : base(message, innerException) => ExitCode = exitcode;
    protected NodeProcessException(int exitcode, SerializationInfo info, StreamingContext context) : base(info, context) => ExitCode = exitcode;
}
