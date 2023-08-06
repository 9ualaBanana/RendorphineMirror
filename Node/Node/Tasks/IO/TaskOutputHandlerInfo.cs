namespace Node.Tasks.IO;

public interface ITaskOutputHandlerInfo
{
    TaskOutputType Type { get; }

    Task UploadResult(ITaskOutputInfo info, object result, CancellationToken token);
    Task OnPlacedTaskCompleted(ILifetimeScope container, ITaskOutputInfo info);

    /// <summary>
    /// Check tasks for completion and returns true if task needs to be set to Finished.
    /// By default returns true if the task state is <see cref="TaskState.Validation"/>.
    /// Assumes all the task properties are updated.
    /// </summary>
    bool CheckCompletion(ILifetimeScope container, ITaskOutputInfo info, TaskState state);
}
public abstract class TaskOutputHandlerInfo<TData, TResult> : ITaskOutputHandlerInfo
    where TData : ITaskOutputInfo
    where TResult : notnull
{
    public abstract TaskOutputType Type { get; }
    protected abstract Type UploadHandlerType { get; }
    protected virtual Type? CompletionCheckerType => null;
    protected virtual Type? CompletedHandlerType => null;

    public async Task UploadResult(ITaskOutputInfo info, object result, CancellationToken token) =>
        await UploadResult((TData) info, (TResult) result, token);

    public async Task UploadResult(ILifetimeScope container, TData info, TResult result, CancellationToken token)
    {
        var logger = container.ResolveLogger(this);
        using var _logscope = logger.BeginScope("Output");
        logger.LogInformation($"Uploading result to {JsonConvert.SerializeObject(result, Formatting.None)}");

        using var ctx = container.ResolveForeign<UploadHandlerBase>(UploadHandlerType, out var handler);
        await handler.UploadResult(info, result, token);
        logger.LogInformation($"Result uploaded");
    }

    public bool CheckCompletion(ILifetimeScope container, ITaskOutputInfo info, TaskState state) =>
        CheckCompletion(container, (TData) info, state);

    public bool CheckCompletion(ILifetimeScope container, TData info, TaskState state)
    {
        using var ctx = container.ResolveForeign<CompletionCheckerBase>(UploadHandlerType, out var handler);
        return handler.CheckCompletion(info, state);
    }

    public async Task OnPlacedTaskCompleted(ILifetimeScope container, ITaskOutputInfo info) =>
        await OnPlacedTaskCompleted(container, (TData) info);

    public async Task OnPlacedTaskCompleted(ILifetimeScope container, TData info)
    {
        using var ctx = container.ResolveForeign<CompletedHandlerBase>(UploadHandlerType, out var handler);
        await handler.OnPlacedTaskCompleted(info);
    }


    protected abstract class UploadHandlerBase
    {
        public required IProgressSetter ProgressSetter { get; init; }
        public required ILogger<UploadHandlerBase> Logger { get; init; }

        public abstract Task UploadResult(TData info, TResult result, CancellationToken token);
    }
    protected abstract class CompletionCheckerBase
    {
        public required ILogger<CompletionCheckerBase> Logger { get; init; }

        public abstract bool CheckCompletion(TData info, TaskState state);
    }
    protected abstract class CompletedHandlerBase
    {
        public required ILogger<CompletedHandlerBase> Logger { get; init; }

        public abstract Task OnPlacedTaskCompleted(TData info);
    }
}
