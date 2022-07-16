using Common.NodeToUI;

namespace Common;

public static class Apis
{
    const string TaskManagerEndpoint = Api.TaskManagerEndpoint;

    public static ValueTask<OperationResult<ImmutableDictionary<PluginType, SoftwareStats>>> GetSoftwareStatsAsync() =>
        Api.ApiGet<ImmutableDictionary<PluginType, SoftwareStats>>($"{TaskManagerEndpoint}/getsoftwarestats", "stats");

    public static ValueTask<OperationResult<TaskFullState>> GetTaskStateAsync(string taskid) =>
        Api.ApiGet<TaskFullState>($"{Api.TaskManagerEndpoint}/getmytaskstate", null, "Getting task state", ("sessionid", Settings.SessionId!), ("taskid", taskid));
}
