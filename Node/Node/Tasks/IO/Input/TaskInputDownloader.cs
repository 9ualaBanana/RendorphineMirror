namespace Node.Tasks.IO.Input;

public interface ITaskInputDownloader
{
    Task<object> Download(ITaskInputInfo input, TaskObject obj, CancellationToken token);
}
public interface ITaskInputDownloader<TInput, TResult> : ITaskInputDownloader
    where TInput : ITaskInputInfo
    where TResult : notnull
{
    Task<TResult> Download(TInput input, TaskObject obj, CancellationToken token);
}

public abstract class TaskInputDownloader<TInput, TResult> : ITaskInputDownloader<TInput, TResult>
    where TInput : ITaskInputInfo
    where TResult : notnull
{
    public required IProgressSetter ProgressSetter { get; init; }
    public required ILogger<TaskInputDownloader<TInput, TResult>> Logger { get; init; }

    async Task<object> ITaskInputDownloader.Download(ITaskInputInfo input, TaskObject obj, CancellationToken token) =>
        await Download((TInput) input, obj, token);

    public virtual async Task<TResult> Download(TInput input, TaskObject obj, CancellationToken token)
    {
        using var _logscope = Logger.BeginScope("Input");
        Logger.LogInformation($"Downloading input from {JsonConvert.SerializeObject(input, Formatting.None)}");

        var result = await DownloadImpl(input, obj, token);
        Logger.LogInformation($"Input downloaded: {result}");

        return result;
    }

    protected abstract Task<TResult> DownloadImpl(TInput input, TaskObject obj, CancellationToken token);
}