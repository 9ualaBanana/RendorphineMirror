namespace Node.Tasks.IO;

public interface ITaskInputHandlerInfo
{
    TaskInputType Type { get; }
    Type ResultType { get; }

    Task<object> Download(ILifetimeScope container, ITaskInputInfo input, TaskObject obj, CancellationToken token);
    Task<OperationResult<TaskObject>> GetTaskObject(ILifetimeScope container, ITaskInputInfo input, CancellationToken token);
    Task UploadInputFiles(ILifetimeScope container, ITaskInputInfo input);
}
public abstract class TaskInputHandlerInfo<TInput, TResult> : ITaskInputHandlerInfo
    where TInput : ITaskInputInfo
    where TResult : notnull
{
    public Type ResultType => typeof(TResult);
    public abstract TaskInputType Type { get; }
    protected abstract Type HandlerType { get; }
    protected abstract Type TaskObjectProviderType { get; }
    protected virtual Type? InputUploaderType => null;

    async Task<object> ITaskInputHandlerInfo.Download(ILifetimeScope container, ITaskInputInfo input, TaskObject obj, CancellationToken token) =>
        await Download(container, (TInput) input, obj, token);

    public async Task<TResult> Download(ILifetimeScope container, TInput input, TaskObject obj, CancellationToken token)
    {
        var logger = container.ResolveLogger(this);
        using var _logscope = logger.BeginScope("Input");
        logger.LogInformation($"Downloading input from {JsonConvert.SerializeObject(input, Formatting.None)}");

        using var ctx = container.ResolveForeign<HandlerBase>(HandlerType, out var handler);
        var result = await handler.Download(input, obj, token);
        logger.LogInformation($"Input downloaded: {result}");

        return result;
    }

    public async Task<OperationResult<TaskObject>> GetTaskObject(ILifetimeScope container, ITaskInputInfo input, CancellationToken token) =>
        await GetTaskObject(container, (TInput) input, token);

    public async Task<OperationResult<TaskObject>> GetTaskObject(ILifetimeScope container, TInput input, CancellationToken token)
    {
        using var ctx = container.ResolveForeign<TaskObjectProviderBase>(TaskObjectProviderType, out var handler);
        return await handler.GetTaskObject(input, token);
    }

    public async Task UploadInputFiles(ILifetimeScope container, ITaskInputInfo input) =>
        await UploadInputFiles(container, (TInput) input);

    public async Task UploadInputFiles(ILifetimeScope container, TInput input)
    {
        if (InputUploaderType is null) return;

        using var ctx = container.ResolveForeign<InputUploaderBase>(InputUploaderType, out var handler);
        await handler.UploadInputFiles(input);
    }


    protected abstract class HandlerBase
    {
        public required IProgressSetter ProgressSetter { get; init; }
        public required ILogger<HandlerBase> Logger { get; init; }

        public abstract Task<TResult> Download(TInput input, TaskObject obj, CancellationToken token);
    }
    protected abstract class TaskObjectProviderBase
    {
        public required ILogger<TaskObjectProviderBase> Logger { get; init; }

        public abstract Task<OperationResult<TaskObject>> GetTaskObject(TInput input, CancellationToken token);
    }
    protected abstract class InputUploaderBase
    {
        public required ILogger<InputUploaderBase> Logger { get; init; }

        public abstract Task UploadInputFiles(TInput input);
    }
}