namespace Node.Tasks.IO.Output;

public interface ITaskUploadHandler
{
    Type ResultType { get; }

    Task UploadResult(ITaskOutputInfo info, object result, CancellationToken token);
}
public interface ITaskUploadHandler<TData, TResult> : ITaskUploadHandler
    where TData : ITaskOutputInfo
    where TResult : notnull
{
    Task UploadResult(TData info, TResult result, CancellationToken token);
}

public abstract class TaskUploadHandler<TData, TResult> : ITaskUploadHandler<TData, TResult>
    where TData : ITaskOutputInfo
    where TResult : notnull
{
    Type ITaskUploadHandler.ResultType => typeof(TResult);

    public required IProgressSetter ProgressSetter { get; init; }
    public required ILogger<TaskUploadHandler<TData, TResult>> Logger { get; init; }

    async Task ITaskUploadHandler.UploadResult(ITaskOutputInfo info, object result, CancellationToken token) =>
        await UploadResult((TData) info, (TResult) result, token);

    public virtual async Task UploadResult(TData info, TResult result, CancellationToken token)
    {
        Logger.LogInformation($"Uploading result to {JsonConvert.SerializeObject(result, Formatting.None)}");
        await UploadResultImpl(info, result, token);
        Logger.LogInformation($"Result uploaded");
    }

    protected abstract Task UploadResultImpl(TData info, TResult result, CancellationToken token);
}
