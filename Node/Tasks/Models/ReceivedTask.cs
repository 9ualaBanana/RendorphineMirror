namespace Node.Tasks.Models;

public record ReceivedTask(string Id, TaskInfo Info)
{
    public RequestOptions RequestOptions { get; set; } = null!;

    public async ValueTask<OperationResult> ChangeStateAsync(TaskState state)
    {
        var result = await Api.ApiGet($"{Api.TaskManagerEndpoint}/mytaskstatechanged", "changing state",
            ("sessionid", Settings.SessionId!), ("taskid", Id), ("newstate", state.ToString().ToLowerInvariant())).ConfigureAwait(false);

        result.LogIfError();
        if (result) Log.Information($"Changing task {Id} status to {state}");
        return result;
    }

    public void LogInfo(string text) => Log.Information($"[Task {Id}] {text}");
    public void LogErr(Exception ex) => Log.Error($"[Task {Id}] {ex}");

    public override string ToString() => $"Task {Id}";
}
