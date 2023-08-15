namespace Node.Common;

public class TaskFailedException : Exception
{
    public override string Message { get; }

    public TaskFailedException(string message) => Message = message;

    [DoesNotReturn] public static void Throw(string message) => throw new TaskFailedException(message);
}