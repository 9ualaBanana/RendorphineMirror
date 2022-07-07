using Common.Tasks.Models;

namespace Node.Tasks;

internal abstract class TaskHandler
{
    protected IncomingTask Task { get; }
    protected RequestOptions RequestOptions { get; set; }

    protected TaskHandler(IncomingTask task, RequestOptions requestOptions)
    {
        Task = task;
        RequestOptions = requestOptions;
    }

    internal async Task HandleAsync()
    {
        var input = await ReceiveInputAsync();
        await ChangeTaskState(TaskState.Active);
        var output = await HandleAsyncCore(input);
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

    protected abstract Task<string> HandleAsyncCore(string input);
    protected abstract Task<string> ReceiveInputAsync();
    protected abstract Task OutputResultAsync(string output);
}
