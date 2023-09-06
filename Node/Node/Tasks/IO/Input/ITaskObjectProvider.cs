namespace Node.Tasks.IO.Input;

public interface ITaskObjectProvider
{
    Task<OperationResult<TaskObject>> GetTaskObject(ITaskInputInfo input, CancellationToken token);
}
public interface ITaskObjectProvider<TInput> : ITaskObjectProvider
    where TInput : ITaskInputInfo
{
    Task<OperationResult<TaskObject>> GetTaskObject(TInput input, CancellationToken token);
}

public abstract class TaskObjectProvider<TInput> : ITaskObjectProvider<TInput>
    where TInput : ITaskInputInfo
{
    public required ILogger<TaskObjectProvider<TInput>> Logger { get; init; }

    async Task<OperationResult<TaskObject>> ITaskObjectProvider.GetTaskObject(ITaskInputInfo input, CancellationToken token) =>
        await GetTaskObject((TInput) input, token);

    public abstract Task<OperationResult<TaskObject>> GetTaskObject(TInput input, CancellationToken token);
}
