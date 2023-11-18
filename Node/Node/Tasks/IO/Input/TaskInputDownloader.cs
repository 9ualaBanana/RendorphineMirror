namespace Node.Tasks.IO.Input;

public interface ITaskInputDownloader
{
    bool AllowOutOfOrderDownloads { get; }

    Task<object> Download(ITaskInputInfo input, TaskObject obj, CancellationToken token);
    async Task<IReadOnlyList<object>> MultiDownload(IEnumerable<ITaskInputInfo> inputs, TaskObject obj, CancellationToken token)
    {
        var result = new List<object>();
        foreach (var input in inputs)
            result.Add(await Download(input, obj, token));

        return result;
    }
}
public interface ITaskInputDownloader<TInput, TResult> : ITaskInputDownloader
    where TInput : ITaskInputInfo
    where TResult : notnull
{
    Task<TResult> Download(TInput input, TaskObject obj, CancellationToken token);
    Task<IReadOnlyList<TResult>> MultiDownload(IEnumerable<TInput> input, TaskObject obj, CancellationToken token);
}

public abstract class TaskInputDownloader<TInput, TResult> : ITaskInputDownloader<TInput, TResult>
    where TInput : ITaskInputInfo
    where TResult : class
{
    public virtual bool AllowOutOfOrderDownloads => false;
    protected virtual bool AllowConcurrentMultiDownload => false;

    public required ITaskProgressSetter ProgressSetter { get; init; }
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

    async Task<IReadOnlyList<object>> ITaskInputDownloader.MultiDownload(IEnumerable<ITaskInputInfo> inputs, TaskObject obj, CancellationToken token) =>
        await MultiDownload(inputs.Cast<TInput>(), obj, token);

    public virtual async Task<IReadOnlyList<TResult>> MultiDownload(IEnumerable<TInput> inputs, TaskObject obj, CancellationToken token)
    {
        var result = new List<TResult>();

        if (AllowConcurrentMultiDownload)
        {
            var results = await Task.WhenAll(inputs.Select(async input => await Download(input, obj, token)));
            result.AddRange(results);
        }
        else
        {
            foreach (var input in inputs)
                result.Add(await Download(input, obj, token));
        }

        return result;
    }

    protected abstract Task<TResult> DownloadImpl(TInput input, TaskObject obj, CancellationToken token);
}
