namespace Node.Tasks;

/// <summary> Exception to throw for the current task to be canceled </summary>
public class NodeTaskFailedException : Exception
{
    public override string Message { get; }
    public string? FullError { get; init; }

    public NodeTaskFailedException(string message) => Message = message;
}

public static class NodeTaskExtensions
{
    [DoesNotReturn]
    public static void ThrowFailed(this TaskBase task, string message, string? fullerr = null) => throw new NodeTaskFailedException(message) { FullError = fullerr };
}