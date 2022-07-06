using Node.Tasks.Models;

namespace Node.Tasks;

internal abstract class TaskHandler
{
    protected IncomingTask Task;
    protected RequestOptions RequestOptions { get; set; }

    protected TaskHandler(IncomingTask task, RequestOptions requestOptions)
    {
        Task = task;
        RequestOptions = requestOptions;
    }

    internal async Task HandleAsync()
    {
        FileInfo input = await ReceiveInputAsync();
        await ChangeTaskState(TaskState.Active);
        FileInfo output = await HandleAsyncCore(input);
        await ChangeTaskState(TaskState.Output);
        await OutputResultAsync(output);
    }

    async Task ChangeTaskState(TaskState state)
    {
        var httpContent = new FormUrlEncodedContent(new Dictionary<string, string>()
        {
            ["sessionid"] = Settings.SessionId!,
            ["taskid"] = Task.TaskId,
            ["newstate"] = Enum.GetName(state)!.ToLower()
        });
        await Api.TrySendRequestAsync(
            async () => await RequestOptions.HttpClient.PostAsync(
                $"{Api.TaskManagerEndpoint}/mytaskstatechanged",
                httpContent),
            RequestOptions);
    }

    protected abstract Task<FileInfo> HandleAsyncCore(FileInfo input);
    protected abstract Task<FileInfo> ReceiveInputAsync();
    protected abstract Task OutputResultAsync(FileInfo output);
}
