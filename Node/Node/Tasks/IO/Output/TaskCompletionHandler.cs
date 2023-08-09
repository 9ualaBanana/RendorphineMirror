namespace Node.Tasks.IO.Output;

public interface ITaskCompletionHandler
{
    Task OnPlacedTaskCompleted(ITaskOutputInfo info);
}
public interface ITaskCompletionHandler<TData> : ITaskCompletionHandler
    where TData : ITaskOutputInfo
{
    Task OnPlacedTaskCompleted(TData info);
}

public abstract class TaskCompletionHandler<TData> : ITaskCompletionHandler<TData>
    where TData : ITaskOutputInfo
{
    public required ILogger<TaskCompletionChecker<TData>> Logger { get; init; }

    async Task ITaskCompletionHandler.OnPlacedTaskCompleted(ITaskOutputInfo info) =>
        await OnPlacedTaskCompleted((TData) info);

    public abstract Task OnPlacedTaskCompleted(TData info);
}
