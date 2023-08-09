namespace Node.Tasks.IO.Output;

public interface ITaskCompletionChecker
{
    bool CheckCompletion(object info, TaskState state);
}
public interface ITaskCompletionChecker<TData> : ITaskCompletionChecker
    where TData : notnull
{
    bool CheckCompletion(TData info, TaskState state);
}

public abstract class TaskCompletionChecker<TData> : ITaskCompletionChecker<TData> where TData : notnull
{
    public required ILogger<TaskCompletionChecker<TData>> Logger { get; init; }

    bool ITaskCompletionChecker.CheckCompletion(object info, TaskState state) =>
        CheckCompletion((TData) info, state);

    public abstract bool CheckCompletion(TData info, TaskState state);
}
