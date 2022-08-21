namespace Common;

public static class Apis
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    const string TaskManagerEndpoint = Api.TaskManagerEndpoint;

    public static ValueTask<OperationResult<ImmutableDictionary<PluginType, SoftwareStats>>> GetSoftwareStatsAsync() =>
        Api.ApiGet<ImmutableDictionary<PluginType, SoftwareStats>>($"{TaskManagerEndpoint}/getsoftwarestats", "stats");

    public static ValueTask<OperationResult<TaskFullState>> GetTaskStateAsync(string taskid, string? sessionId = default) =>
        Api.ApiGet<TaskFullState>($"{Api.TaskManagerEndpoint}/getmytaskstate", null, "Getting task state", ("sessionid", sessionId ?? Settings.SessionId!), ("taskid", taskid));

    public static ValueTask<OperationResult<ImmutableArray<NodeInfo>>> GetMyNodesAsync(string? sessionid = null) =>
        Api.ApiGet<ImmutableArray<NodeInfo>>($"{TaskManagerEndpoint}/getmynodes", "nodes", "Getting my nodes", ("sessionid", sessionid ?? Settings.SessionId!));

    public static ValueTask<OperationResult<TaskFullState>> GetTaskStateAsync(this PlacedTask task, string? sessionId = default) => GetTaskStateAsync(task.Id, sessionId);
    public static async ValueTask<OperationResult> ChangeStateAsync(this ReceivedTask task, TaskState state, string? sessionId = default)
    {
        task.LogInfo($"State changed to {state}");
        if (task.ExecuteLocally) return true;

        var result = await Api.ApiGet($"{Api.TaskManagerEndpoint}/mytaskstatechanged", "changing state",
            ("sessionid", sessionId ?? Settings.SessionId!), ("taskid", task.Id), ("newstate", state.ToString().ToLowerInvariant())).ConfigureAwait(false);

        result.LogIfError();
        return result;
    }
}
