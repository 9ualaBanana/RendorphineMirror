using System.Diagnostics.CodeAnalysis;

namespace Node.Tasks;

/// <summary> Exception to throw for the current task to be canceled </summary>
public class NodeTaskFailedException : Exception
{
    public override string Message { get; }

    public NodeTaskFailedException(string message) => Message = message;
}

public static class NodeTaskExtensions
{
    [DoesNotReturn]
    public static void ThrowFailed(this TaskBase task, string message) => throw new NodeTaskFailedException(message);
}