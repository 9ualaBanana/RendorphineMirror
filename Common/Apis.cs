namespace Common;

public static class Apis
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    const string TaskManagerEndpoint = Api.TaskManagerEndpoint;

    public static ValueTask<OperationResult<TaskFullState>> GetTaskStateAsync(string taskid, string? sessionId = default) =>
        Api.ApiGet<TaskFullState>($"{Api.TaskManagerEndpoint}/getmytaskstate", null, "Getting task state", ("sessionid", sessionId ?? Settings.SessionId!), ("taskid", taskid));

    public static async ValueTask<OperationResult<ImmutableArray<DbTaskFullState>>> GetMyTasksAsync(TaskState[] states, string? afterId = null, string? sessionId = default) =>
        (await Task.WhenAll(states.Select(async s => await GetMyTasksAsync(s, afterId, sessionId)))).MergeResults().Next(x => x.SelectMany(x => x).ToImmutableArray().AsOpResult());

    public static ValueTask<OperationResult<ImmutableArray<DbTaskFullState>>> GetMyTasksAsync(TaskState state, string? afterId = null, string? sessionId = default) =>
        Api.ApiGet<ImmutableArray<DbTaskFullState>>($"{Api.TaskManagerEndpoint}/gettasklist", "list", "Getting task list",
            ("sessionid", sessionId ?? Settings.SessionId!), ("state", state.ToString().ToLowerInvariant()), ("afterid", afterId ?? string.Empty), ("alltasks", "0"));

    public static ValueTask<OperationResult<ImmutableArray<NodeInfo>>> GetMyNodesAsync(string? sessionid = null) =>
        Api.ApiGet<ImmutableArray<NodeInfo>>($"{TaskManagerEndpoint}/getmynodes", "nodes", "Getting my nodes", ("sessionid", sessionid ?? Settings.SessionId!));
    public static ValueTask<OperationResult<NodeInfo>> GetNodeAsync(string nodeid, string? sessionid = null) =>
        GetMyNodesAsync(sessionid).Next(nodes => nodes.FirstOrDefault(x => x.Id == nodeid)?.AsOpResult() ?? OperationResult.Err($"Node with such id ({nodeid}) was not found"));

    public static ValueTask<OperationResult<ImmutableDictionary<string, SoftwareDefinition>>> GetSoftwareAsync() =>
        LocalApi.Send<ImmutableDictionary<string, SoftwareDefinition>>(Settings.RegistryUrl, "getsoft")
        .Next(x => x.WithComparers(StringComparer.OrdinalIgnoreCase).AsOpResult());


    public static async ValueTask<OperationResult<TaskFullState>> GetTaskStateAsync(this ReceivedTask task, string? sessionId = default)
    {
        var state = await GetTaskStateAsync(task.Id, sessionId);
        if (state)
        {
            if (task.State != state.Result.State)
                task.LogInfo($"Placed task state changed to {state.Result.State}");

            task.State = state.Result.State;
            task.Progress = state.Result.Progress;
            if (task is DbTaskFullState dbtask)
                dbtask.Server = state.Result.Server;
        }

        return state;
    }
    public static async ValueTask<OperationResult> ChangeStateAsync(this ReceivedTask task, TaskState state, string? sessionId = default)
    {
        task.LogInfo($"Changing state to {state}");
        if (task.ExecuteLocally) return true;

        var result = await Api.ApiGet($"{Api.TaskManagerEndpoint}/mytaskstatechanged", "changing state",
            ("sessionid", sessionId ?? Settings.SessionId!), ("taskid", task.Id), ("newstate", state.ToString().ToLowerInvariant())).ConfigureAwait(false);

        result.LogIfError($"[{(task as ILoggable).LogName}] Error while changing task state: {{0}}");
        if (result) task.State = state;

        return result;
    }
}
