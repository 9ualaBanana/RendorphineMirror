using System.Diagnostics.CodeAnalysis;

namespace Node.Tasks;

/// <summary> Exception to throw for the current task to be canceled </summary>
public class NodeTaskCanceledException : Exception
{
    public override string Message { get; }

    public NodeTaskCanceledException(string message) => Message = message;
}
public static class NodeTaskCanceledExceptionExtensions
{
    [DoesNotReturn]
    public static void ThrowCancel(this ReceivedTask task, string message) => throw new NodeTaskCanceledException(message);
}