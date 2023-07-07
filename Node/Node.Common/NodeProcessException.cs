using System.Runtime.Serialization;

namespace Node.Common;

public class NodeProcessException : Exception
{
    public int ExitCode => Process.ExitCode;
    readonly Process Process;

    public NodeProcessException(Process process) => Process = process;
    public NodeProcessException(Process process, string? message) : base(message) => Process = process;
    public NodeProcessException(Process process, string? message, Exception? innerException) : base(message, innerException) => Process = process;
    protected NodeProcessException(Process process, SerializationInfo info, StreamingContext context) : base(info, context) => Process = process;
}
