namespace Common;

public static class Apis
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    const string TaskManagerEndpoint = Api.TaskManagerEndpoint;

    public static ValueTask<OperationResult<TaskFullState>> GetTaskStateAsync(string taskid, string? sessionId = default) =>
        Api.ApiGet<TaskFullState>($"{Api.TaskManagerEndpoint}/getmytaskstate", null, "Getting task state", ("sessionid", sessionId ?? Settings.SessionId!), ("taskid", taskid));

    public static ValueTask<OperationResult<ImmutableArray<DbTaskFullState>>> GetMyTasksAsync(TaskState state, string? afterId = null, string? sessionId = default) =>
        Api.ApiGet<ImmutableArray<DbTaskFullState>>($"{Api.TaskManagerEndpoint}/gettasklist", "list", "Getting task list",
            ("sessionid", sessionId ?? Settings.SessionId!), ("state", state.ToString().ToLowerInvariant()), ("afterid", afterId ?? string.Empty), ("alltasks", "0"));

    public static ValueTask<OperationResult<ImmutableArray<NodeInfo>>> GetMyNodesAsync(string? sessionid = null) =>
        Api.ApiGet<ImmutableArray<NodeInfo>>($"{TaskManagerEndpoint}/getmynodes", "nodes", "Getting my nodes", ("sessionid", sessionid ?? Settings.SessionId!));

    public static ValueTask<OperationResult<ImmutableDictionary<string, SoftwareDefinition>>> GetSoftwareAsync() =>
        LocalApi.Send<ImmutableDictionary<string, SoftwareDefinition>>(Settings.RegistryUrl, "getsoft")
        .Next(x => x.WithComparers(StringComparer.OrdinalIgnoreCase).AsOpResult());

    public static ValueTask<OperationResult<TaskFullState>> GetTaskStateAsync(this ReceivedTask task, string? sessionId = default) => GetTaskStateAsync(task.Id, sessionId);
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
