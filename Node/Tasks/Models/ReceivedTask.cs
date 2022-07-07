using Node.Tasks.Executor;

namespace Node.Tasks.Models;

public record ReceivedTask(string Id, TaskInfo Info)
{
    internal RequestOptions RequestOptions { get; set; } = null!;

    internal async Task HandleAsync()
    {
        TaskHandler taskHandler;
        if (HasMPlusInput) taskHandler = new MPlusTaskHandler(this, RequestOptions);
        else return;
        await taskHandler.HandleAsync();
    }

    internal async Task ChangeStateAsync(TaskState state)
    {
        var httpContent = new FormUrlEncodedContent(new Dictionary<string, string>()
        {
            ["sessionid"] = Settings.SessionId!,
            ["taskid"] = Id,
            ["newstate"] = Enum.GetName(state)!.ToLower()
        });
        await Api.TryPostAsync(
            $"{Api.TaskManagerEndpoint}/mytaskstatechanged",
            httpContent,
            RequestOptions);
    }

    internal bool HasMPlusInput => Info.Input.GetProperty("type").GetString() == "MPlus";
}
