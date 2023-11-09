namespace Node.Tasks.IO.Output;

public interface ITaskUploadHandler
{
    Type ResultType { get; }

    Task UploadResult(ITaskOutputInfo info, IReadOnlyList<ITaskInputInfo> input, object result, CancellationToken token);
}
public interface ITaskUploadHandler<TData, TResult> : ITaskUploadHandler
    where TData : ITaskOutputInfo
    where TResult : notnull
{
    Task UploadResult(TData info, IReadOnlyList<ITaskInputInfo> input, TResult result, CancellationToken token);
}

public abstract class TaskUploadHandler<TData, TResult> : ITaskUploadHandler<TData, TResult>
    where TData : ITaskOutputInfo
    where TResult : notnull
{
    Type ITaskUploadHandler.ResultType => typeof(TResult);

    public required ITaskProgressSetter ProgressSetter { get; init; }
    public required ILogger<TaskUploadHandler<TData, TResult>> Logger { get; init; }

    async Task ITaskUploadHandler.UploadResult(ITaskOutputInfo info, IReadOnlyList<ITaskInputInfo> input, object result, CancellationToken token)
    {
        if (result is TResult tresult)
            await UploadResult((TData) info, input, tresult, token);
        else if (result is IEnumerable<TResult> tresults)
            foreach (var (tresultt, tinput) in tresults.Zip(input))
                await UploadResult((TData) info, tinput, tresultt, token);
    }

    public async Task UploadResult(TData info, IReadOnlyList<ITaskInputInfo> input, TResult result, CancellationToken token) =>
        await UploadResultImpl(info, input.Single(), result, token);

    public virtual async Task UploadResult(TData info, ITaskInputInfo input, TResult result, CancellationToken token)
    {
        Logger.LogInformation($"Uploading result to {JsonConvert.SerializeObject(result, Formatting.None)}");
        await UploadResultImpl(info, input, result, token);
        Logger.LogInformation($"Result uploaded");
    }

    protected abstract Task UploadResultImpl(TData info, ITaskInputInfo input, TResult result, CancellationToken token);
}
