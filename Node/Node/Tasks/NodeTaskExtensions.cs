namespace Node.Tasks;

public static class NodeTaskExtensions
{
    [DoesNotReturn]
    public static void ThrowFailed(this TaskBase task, string message, string? fullerr = null) => throw new TaskFailedException(message) { FullError = fullerr };
}