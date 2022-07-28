using Common.NodeToUI;

namespace Common;

public static class Apis
{
    const string TaskManagerEndpoint = Api.TaskManagerEndpoint;

    public static ValueTask<OperationResult<ImmutableDictionary<PluginType, SoftwareStats>>> GetSoftwareStatsAsync() =>
        Api.ApiGet<ImmutableDictionary<PluginType, SoftwareStats>>($"{TaskManagerEndpoint}/getsoftwarestats", "stats");

    public static ValueTask<OperationResult<TaskFullState>> GetTaskStateAsync(string taskid) =>
        Api.ApiGet<TaskFullState>($"{Api.TaskManagerEndpoint}/getmytaskstate", null, "Getting task state", ("sessionid", Settings.SessionId!), ("taskid", taskid));

    public static ValueTask<OperationResult<ImmutableArray<NodeInfo>>> GetMyNodesAsync(string? sessionid = null) =>
        Api.ApiGet<ImmutableArray<NodeInfo>>($"{TaskManagerEndpoint}/getmynodes", "nodes", "Getting my nodes", ("sessionid", sessionid ?? Settings.SessionId!));

    public static async ValueTask<OperationResult> ChangeStateAsync(this ReceivedTask task, TaskState state)
    {
        var result = await Api.ApiGet($"{Api.TaskManagerEndpoint}/mytaskstatechanged", "changing state",
            ("sessionid", Settings.SessionId!), ("taskid", task.Id), ("newstate", state.ToString().ToLowerInvariant())).ConfigureAwait(false);

        result.LogIfError();
        if (result) Log.Information($"Changing task {task.Id} status to {state}");
        return result;
    }
}
