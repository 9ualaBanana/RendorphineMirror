using Node.Tasks.Models;

namespace Node.Tasks.Executor;

internal abstract class TaskHandler
{
    readonly protected ReceivedTask Task;
    protected RequestOptions RequestOptions;

    protected TaskHandler(ReceivedTask task, RequestOptions requestOptions)
    {
        Task = task;
        RequestOptions = requestOptions;
    }

    internal async Task HandleAsync()
    {
        FileInfo input = await ReceiveInputAsync();
        await Task.ChangeStateAsync(TaskState.Active);
        FileInfo output = await HandleAsyncCore(input);
        await Task.ChangeStateAsync(TaskState.Output);
        await OutputResultAsync(output);
    }

    protected abstract Task<FileInfo> ReceiveInputAsync();
    protected abstract Task<FileInfo> HandleAsyncCore(FileInfo input);
    protected abstract Task OutputResultAsync(FileInfo output);
}
