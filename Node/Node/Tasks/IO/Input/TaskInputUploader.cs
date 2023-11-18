namespace Node.Tasks.IO.Input;

public interface ITaskInputUploader
{
    Task Upload(ITaskInputInfo input);
}
public interface ITaskInputUploader<TInput> : ITaskInputUploader
    where TInput : ITaskInputInfo
{
    Task Upload(TInput input);
}


public abstract class TaskInputUploader<TInput> : ITaskInputUploader<TInput>
    where TInput : ITaskInputInfo
{
    public required ILogger<TaskInputUploader<TInput>> Logger { get; init; }

    async Task ITaskInputUploader.Upload(ITaskInputInfo input) =>
        await Upload((TInput) input);

    public abstract Task Upload(TInput input);
}