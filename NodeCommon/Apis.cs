using System.Diagnostics;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NodeCommon;

public static class Apis
{
    public static ApisInstance Default => ApisInstance.Default;
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    const string TaskManagerEndpoint = Api.TaskManagerEndpoint;



    static (string, string)[] AddSessionId((string, string)[] values)
    {
        if (values.Any(x => x.Item1 == "sessionid")) return values;
        return values.Append(("sessionid", Settings.SessionId)).ToArray();
    }

    public static ValueTask<OperationResult> ShardGet(this HttpClient client, ITaskApi task, string url, string errorDetails, params (string, string)[] values) =>
        client.ShardGet<JToken>(task, url, null, errorDetails, values).Next(j => true);
    public static ValueTask<OperationResult> ShardPost(this HttpClient client, ITaskApi task, string url, string errorDetails, params (string, string)[] values) =>
        client.ShardPost<JToken>(task, url, null, errorDetails, values).Next(j => true);
    public static ValueTask<OperationResult<T>> ShardGet<T>(this HttpClient client, ITaskApi task, string url, string? property, string errorDetails, params (string, string)[] values) =>
        task.ShardSend(url, url => client.ApiGet<T>(url, property, errorDetails, AddSessionId(values)));
    public static ValueTask<OperationResult<T>> ShardPost<T>(this HttpClient client, ITaskApi task, string url, string? property, string errorDetails, params (string, string)[] values) =>
        task.ShardSend(url, url => client.ApiPost<T>(url, property, errorDetails, Api.ToContent(AddSessionId(values))));

    public static ValueTask<OperationResult> ShardPost(this HttpClient client, ITaskApi task, string url, string? property, string errorDetails, HttpContent content) =>
        client.ShardPost<JToken>(task, url, property, errorDetails, content).Next(j => true);
    public static ValueTask<OperationResult<T>> ShardPost<T>(this HttpClient client, ITaskApi task, string url, string? property, string errorDetails, HttpContent content) =>
        task.ShardSend(url, url => client.ApiPost<T>(url, property, errorDetails, content));

    public static ValueTask<OperationResult> ShardGet(this ITaskApi task, string url, string errorDetails, params (string, string)[] values) =>
        Api.Client.ShardGet(task, url, errorDetails, values);
    public static ValueTask<OperationResult> ShardPost(this ITaskApi task, string url, string errorDetails, params (string, string)[] values) =>
        Api.Client.ShardPost(task, url, errorDetails, values);
    public static ValueTask<OperationResult<T>> ShardGet<T>(this ITaskApi task, string url, string? property, string errorDetails, params (string, string)[] values) =>
        Api.Client.ShardGet<T>(task, url, property, errorDetails, values);
    public static ValueTask<OperationResult<T>> ShardPost<T>(this ITaskApi task, string url, string? property, string errorDetails, params (string, string)[] values) =>
        Api.Client.ShardPost<T>(task, url, property, errorDetails, values);

    static async ValueTask<OperationResult<T>> ShardSend<T>(this ITaskApi task, string url, Func<string, ValueTask<OperationResult<T>>> func)
    {
        (task as ILoggable)?.LogTrace($"Sending shard request {url}; Shard is {task.HostShard ?? "<null>"}");
        return await ShardSend(task, () => func($"https://{task.HostShard}/rphtasklauncher/{url}"), true);


        static async ValueTask<OperationResult<T>> ShardSend(ITaskApi task, Func<ValueTask<OperationResult<T>>> func, bool tryDefaultShard)
        {
            if (tryDefaultShard)
            {
                task.HostShard ??= "tasks.microstock.plus";
                tryDefaultShard = false;
            }

            if (task.HostShard is null)
            {
                var host = await task.UpdateTaskShardAsync();
                if (!host) return host;
            }

            var result = await OperationResult.WrapException(func);
            if (result) return result;

            // only if an API error
            if (result.GetResult().HttpData is { } httpdata)
            {
                (task as ILoggable)?.LogErr($"Got error {httpdata.ToString().ReplaceLineEndings(" ")} in ({result.Message})");

                // if nonsuccess, refetch shard host, retry
                if (!httpdata.IsSuccessStatusCode)
                {
                    await Task.Delay(30_000);
                    await task.UpdateTaskShardAsync();
                    return await ShardSend(task, func, tryDefaultShard);
                }

                // "No shard is known for this task. The shard could be restarting, try again in 30 seconds"
                if (httpdata.ErrorCode == ErrorCodes.Error && result.Message!.Contains("shard", StringComparison.OrdinalIgnoreCase))
                {
                    var host = await task.UpdateTaskShardAsync();
                    if (!host) return host;

                    await Task.Delay(30_000);
                    return await ShardSend(task, func, tryDefaultShard);
                }
            }

            return result;
        }
    }


    /// <inheritdoc cref="GetTaskShardAsync"/>
    public static async ValueTask<OperationResult> UpdateTaskShardAsync(this ITaskApi task, string? sessionId = default) =>
        await Api.Client.UpdateTaskShardAsync(task, sessionId);
    /// <inheritdoc cref="GetTaskShardAsync"/>
    public static ValueTask<OperationResult> UpdateTaskShardAsync(this HttpClient httpClient, ITaskApi task, string? sessionId = default) =>
        httpClient.GetTaskShardAsync(task.Id, sessionId)
            .Next(s =>
            {
                (task as ILoggable)?.LogTrace($"Shard was updated to {s}");
                task.HostShard = s;
                return true;
            });

    /// <summary> Get shard host for a task. Might take a long time to process. Should never return an error, but who knows... </summary>
    public static async ValueTask<OperationResult<string>> GetTaskShardAsync(string taskid, string? sessionId = default) =>
        await Api.Client.GetTaskShardAsync(taskid, sessionId);
    /// <inheritdoc cref="GetTaskShardAsync"/>
    public static async ValueTask<OperationResult<string>> GetTaskShardAsync(this HttpClient httpClient, string taskid, string? sessionId = default)
    {
        var shard = await httpClient.ApiGet<string>($"{Api.TaskManagerEndpoint}/gettaskshard", "host", "Getting task shard", ("sessionid", sessionId ?? Settings.SessionId!), ("taskid", taskid));

        if (!shard)
        {
            var httpdata = shard.GetResult().HttpData;
            if (httpdata is null) return shard;

            if (!httpdata.Value.IsSuccessStatusCode)
            {
                await Task.Delay(30_000);
                return await httpClient.GetTaskShardAsync(taskid, sessionId);
            }
        }

        // TODO: fix -72 check
        if (!shard && shard.Message!.Contains("-72 error code", StringComparison.Ordinal))
        {
            await Task.Delay(30_000);
            return await httpClient.GetTaskShardAsync(taskid, sessionId);
        }

        return shard;
    }


    public static async ValueTask<OperationResult<ImmutableArray<string>>> GetShardListAsync(string? sessionId = default) =>
        await Api.ApiGet<ImmutableArray<string>>($"{Api.TaskManagerEndpoint}/getshardlist", "list", "Getting shards list", ("sessionid", sessionId ?? Settings.SessionId!));

    public static async ValueTask<OperationResult<Dictionary<string, string>>> GetTaskShardsAsync(IEnumerable<string> taskids, string? sessionId = default) =>
        await Api.Client.GetTaskShardsAsync(taskids, sessionId);
    public static async ValueTask<OperationResult<Dictionary<string, string>>> GetTaskShardsAsync(this HttpClient httpClient, IEnumerable<string> taskids, string? sessionId = default)
    {
        var sel = async (IEnumerable<string> ids) => await httpClient.ApiGet<Dictionary<string, string>>($"{Api.TaskManagerEndpoint}/gettaskshards", "hosts", "Getting tasks shards",
            ("sessionid", sessionId ?? Settings.SessionId!), ("taskids", JsonConvert.SerializeObject(ids)));

        return await taskids.Chunk(100)
            .Select(sel)
            .MergeDictResults();
    }


    /// <summary> Updates <see cref="task.HostShard"/> values. Does NOT guarantee that all tasks provided will have shards </summary>
    /// <returns> Tasks with successfully updated shards </returns>
    public static async ValueTask<OperationResult<DbTaskFullState[]>> UpdateTaskShardsAsync(IEnumerable<DbTaskFullState> tasks, string? sessionId = default) =>
        await Api.Client.UpdateTaskShardsAsync(tasks, sessionId);
    /// <inheritdoc cref="UpdateTaskShardsAsync"/>
    public static async ValueTask<OperationResult<DbTaskFullState[]>> UpdateTaskShardsAsync(IReadOnlyDictionary<string, DbTaskFullState> tasks, string? sessionId = default) =>
        await Api.Client.UpdateTaskShardsAsync(tasks, sessionId);

    /// <inheritdoc cref="UpdateTaskShardsAsync"/>
    public static async ValueTask<OperationResult<DbTaskFullState[]>> UpdateTaskShardsAsync(this HttpClient httpClient, IEnumerable<DbTaskFullState> tasks, string? sessionId = default) =>
        await httpClient.UpdateTaskShardsAsync(tasks.ToDictionary(x => x.Id), sessionId);
    /// <inheritdoc cref="UpdateTaskShardsAsync"/>
    public static async ValueTask<OperationResult<DbTaskFullState[]>> UpdateTaskShardsAsync(this HttpClient httpClient, IReadOnlyDictionary<string, DbTaskFullState> tasks, string? sessionId = default)
    {
        if (tasks.Count == 0) return Array.Empty<DbTaskFullState>();
        return await httpClient.GetTaskShardsAsync(tasks.Keys, sessionId)
            .Next(shards =>
            {
                var ret = new Dictionary<string, DbTaskFullState>();
                foreach (var (id, shard) in shards)
                {
                    tasks[id].HostShard = shard;
                    ret[id] = tasks[id];
                }

                return ret.Values.ToArray().AsOpResult();
            });
    }

    public static async ValueTask<OperationResult> UpdateAllTaskShardsAsync(IEnumerable<DbTaskFullState> tasks, string? sessionId = default) =>
        await Api.Client.UpdateAllTaskShardsAsync(tasks, sessionId);
    /// <summary> Updates <see cref="task.HostShard"/> values. Does not return until all shards are fetched </summary>
    public static async ValueTask<OperationResult> UpdateAllTaskShardsAsync(this HttpClient httpClient, IEnumerable<DbTaskFullState> tasks, string? sessionId = default)
    {
        const int maxmin = 5;

        var tasksdict = tasks.ToDictionary(x => x.Id);
        tasks = null!;

        var start = true;
        var sw = Stopwatch.StartNew();
        while (true)
        {
            if (start) start = false;
            else await Task.Delay(10_000);

            if (tasksdict.Count == 0) return true;
            if (sw.Elapsed.TotalMinutes > maxmin)
                return OperationResult.Err("Could not get all tasks shards");

            var shards = await httpClient.GetTaskShardsAsync(tasksdict.Keys, sessionId);
            shards.LogIfError();
            if (!shards) continue;

            foreach (var (id, shard) in shards.Value)
            {
                tasksdict[id].HostShard = shard;
                tasksdict.Remove(id);
            }
        }
    }


    /// <returns> Input, Active, Output tasks on a shard </returns>
    public static async ValueTask<OperationResult<TMTasksStateInfo>> GetTasksOnShardAsync(string shardhost, string? sessionId = default) =>
        await Api.Client.GetTasksOnShardAsync(shardhost, sessionId);
    /// <inheritdoc cref="GetTasksOnShardAsync(string, string)"/>
    public static async ValueTask<OperationResult<TMTasksStateInfo>> GetTasksOnShardAsync(this HttpClient httpClient, string shardhost, string? sessionId = default) =>
        await httpClient.ApiGet<TMTasksStateInfo>($"https://{shardhost}/rphtasklauncher/getmytasksinfo", null, "Getting shard tasks", ("sessionid", sessionId ?? Settings.SessionId!));

    public static async ValueTask<OperationResult<(TaskState state, TMTaskStateInfo info)[]>> GetTasksOnShardsAsync(IEnumerable<string> shards, string? sessionId = default) =>
        await Api.Client.GetTasksOnShardsAsync(shards, sessionId);
    public static async ValueTask<OperationResult<(TaskState state, TMTaskStateInfo info)[]>> GetTasksOnShardsAsync(this HttpClient httpClient, IEnumerable<string> shards, string? sessionId = default)
    {
        var result = await shards.Select(async shard => await httpClient.GetTasksOnShardAsync(shard, sessionId)).MergeResults();
        if (!result) return result.GetResult();

        var itasks = result.Value.SelectMany(x => x.Input).Select(x => (TaskState.Input, x));
        var atasks = result.Value.SelectMany(x => x.Active).Select(x => (TaskState.Active, x));
        var otasks = result.Value.SelectMany(x => x.Output).Select(x => (TaskState.Output, x));
        return itasks.Concat(atasks).Concat(otasks).ToArray();
    }
    public record TMTasksStateInfo(ImmutableArray<TMTaskStateInfo> Input, ImmutableArray<TMTaskStateInfo> Active, ImmutableArray<TMTaskStateInfo> Output, int QueueSize, double AvgWaitTime, string ScGuid);
    public record TMTaskStateInfo(string Id, double Progress);


    /// <returns> Finished, Failed and Canceled tasks </returns>
    public static async ValueTask<OperationResult<Dictionary<string, TMOldTaskStateInfo>>> GetFinishedTasksStatesAsync(IEnumerable<string> taskids, string? sessionId = default) =>
        await Api.Client.GetFinishedTasksStatesAsync(taskids, sessionId);
    /// <inheritdoc cref="GetFinishedTasksStatesAsync"/>
    public static async ValueTask<OperationResult<Dictionary<string, TMOldTaskStateInfo>>> GetFinishedTasksStatesAsync(this HttpClient httpClient, IEnumerable<string> taskids, string? sessionId = default)
    {
        var sel = async (IEnumerable<string> ids) => await httpClient.ApiPost<Dictionary<string, TMOldTaskStateInfo>>($"{Api.TaskManagerEndpoint}/gettasksstate", "tasks", "Getting finished tasks states",
            ("sessionid", sessionId ?? Settings.SessionId!), ("taskids", JsonConvert.SerializeObject(ids)));

        return await taskids.Chunk(100)
            .Select(sel)
            .MergeDictResults();
    }
    public record TMOldTaskStateInfo(string Id, TaskState State, ITaskOutputInfo? Output, string? ErrMsg);


    /// <returns> Task state or null if the task is Finished/Canceled/Failed </returns>
    public static async ValueTask<OperationResult<ServerTaskState?>> GetTaskStateAsync(this ITaskApi task, string? sessionId = default)
    {
        var get = () => task.ShardGet<ServerTaskState>("getmytaskstate", null, $"Getting {task.Id} task state", ("sessionid", sessionId ?? Settings.SessionId!), ("taskid", task.Id));
        bool exists(string errmsg) => !errmsg.Contains("There is no task with such ID", StringComparison.Ordinal) && !errmsg.Contains("No shard known for this task", StringComparison.Ordinal);


        // (completed in this case means finished, canceled or failed)
        // Completed tasks are not being stored on shards anymore, so if we get `There is no task with such ID` error, the task probably was completed
        // On this error, we refetch the task shard and retry the request on that shard. If the same error is returned, the task was completed and we return null

        var state = await get();
        if (!state && !exists(state.Message!))
        {
            var update = await task.UpdateTaskShardAsync(sessionId);
            if (!update) return update;

            state = await get();
            if (!state && !exists(state.Message!))
                return null as ServerTaskState;
        }

        if (task is TaskBase rtask)
            rtask.SetStateTime(state.Value.State);
        return state!;
    }

    /// <returns> Task state; Throws if task is Finished/Canceled/Failed. </returns>
    public static async ValueTask<OperationResult<ServerTaskState>> GetTaskStateAsyncOrThrow(this ITaskApi task, string? sessionId = default) =>
        await task.GetTaskStateAsync(sessionId)
            .Next(state => state.ThrowIfNull($"Task {task.Id} is already completed").AsOpResult());
    public record ServerTaskState(TaskState State, double Progress, ITaskOutputInfo Output, TaskTimes Times, TaskServer? Server = null);


    public static ValueTask<OperationResult> FailTaskAsync(this ITaskApi task, string errorMessage, string? sessionId = default) => ChangeStateAsync(task, TaskState.Failed, errorMessage, sessionId);
    public static ValueTask<OperationResult> ChangeStateAsync(this ITaskApi task, TaskState state, string? sessionId = default) => ChangeStateAsync(task, state, null, sessionId);
    static async ValueTask<OperationResult> ChangeStateAsync(this ITaskApi task, TaskState state, string? errorMessage, string? sessionId = default)
    {
        (task as ILoggable)?.LogInfo($"Changing state to {state}");


        var data = new[]
        {
            ("sessionid", sessionId ?? Settings.SessionId!),
            ("taskid", task.Id),
            ("newstate", state.ToString().ToLowerInvariant()),
        };

        if (errorMessage is not null)
        {
            if (state != TaskState.Failed)
                throw new ArgumentException($"Could not provide {nameof(errorMessage)} for task state {state}");

            data = data.Append(("errormessage", errorMessage)).ToArray();
        }

        var result = await task.ShardGet("mytaskstatechanged", "Changing task state", data).ConfigureAwait(false);


        result.LogIfError($"[{(task as ILoggable)?.LogName ?? task.Id}] Error while changing task state: {{0}}");
        if (result && task is TaskBase rtask)
        {
            rtask.State = state;
            rtask.SetStateTime(state);
        }

        return result;
    }

    /// <summary> Send current task progress to the server </summary>
    public static ValueTask<OperationResult> SendTaskProgressAsync(this ReceivedTask task, string? sessionId = default) =>
        task.ShardGet("mytaskprogress", "Sending task progress",
            ("sessionid", sessionId ?? Settings.SessionId!), ("taskid", task.Id), ("curstate", task.State.ToString().ToLowerInvariant()), ("progress", task.Progress.ToString()));



    /// <returns> ALL user tasks by states, might take a while </returns>
    public static ValueTask<OperationResult<Dictionary<TaskState, List<ServerTaskFullState>>>> GetAllMyTasksAsync(TaskState[] states, string? sessionId = default)
    {
        return GetShardListAsync(sessionId)
            .Next(shards => OperationResult.WrapException(() => states.Select(async state => (state, (await next(shards, state, null)).ToList()).AsOpResult()).MergeDictResults()));


        async ValueTask<IEnumerable<ServerTaskFullState>> next(ImmutableArray<string> shards, TaskState state, string? afterId)
        {
            var tasks = await GetMyTasksAsync(shards, state, afterId, sessionId).ThrowIfError();
            if (tasks.Count <= 1) return tasks;

            return tasks.Concat(await next(shards, state, tasks.Max(x => x.Id)));
        }
    }
    /// <returns> User tasks by state, up to 500 per state </returns>
    public static ValueTask<OperationResult<List<ServerTaskFullState>>> GetMyTasksAsync(TaskState[] states, string? afterId = null, string? sessionId = default) =>
        GetShardListAsync(sessionId)
            .Next(shards => states.Select(async state => await GetMyTasksAsync(shards, state, afterId, sessionId)).MergeArrResults());
    /// <returns> User tasks by state, up to 500 </returns>
    static ValueTask<OperationResult<List<ServerTaskFullState>>> GetMyTasksAsync(TaskState state, string? afterId = null, string? sessionId = default) =>
        GetShardListAsync(sessionId)
            .Next(shards => GetMyTasksAsync(shards, state, afterId, sessionId));
    /// <inheritdoc cref="GetMyTasksAsync(TaskState, string?, string?)"/>
    static async ValueTask<OperationResult<List<ServerTaskFullState>>> GetMyTasksAsync(IReadOnlyCollection<string> shards, TaskState state, string? afterId = null, string? sessionId = default)
    {
        var getfunc = (string shard) => Api.ApiGet<List<ServerTaskFullState>>($"https://{shard}/rphtasklauncher/gettasklist", "list", "Getting task list",
            ("sessionid", sessionId ?? Settings.SessionId!), ("state", state.ToString().ToLowerInvariant()), ("afterid", afterId ?? string.Empty));

        return await shards.Select(async shard => await getfunc(shard)).MergeArrResults();
    }
    public record ServerTaskFullState : DbTaskFullState, ILoggable
    {
        public TaskServer? Server { get; set; }

        [JsonConstructor]
        public ServerTaskFullState(string id, string originGuid, TaskPolicy launchPolicy, TaskObject @object, ITaskInputInfo input, ITaskOutputInfo output, JObject data)
            : base(id, new TaskInfo(@object, input, output, data, launchPolicy, originGuid)) { }
    }


    public static ValueTask<OperationResult<ImmutableArray<NodeInfo>>> GetMyNodesAsync(string? sessionid = null) =>
        Api.ApiGet<ImmutableArray<NodeInfo>>($"{TaskManagerEndpoint}/getmynodes", "nodes", "Getting my nodes", ("sessionid", sessionid ?? Settings.SessionId!));
    public static ValueTask<OperationResult<NodeInfo>> GetNodeAsync(string nodeid, string? sessionid = null) =>
        GetMyNodesAsync(sessionid).Next(nodes => nodes.FirstOrDefault(x => x.Id == nodeid)?.AsOpResult() ?? OperationResult.Err($"Node with such id ({nodeid}) was not found"));

    public static ValueTask<OperationResult<ImmutableDictionary<string, SoftwareDefinition>>> GetSoftwareAsync() =>
        LocalApi.Send<ImmutableDictionary<string, SoftwareDefinition>>(Settings.RegistryUrl, "getsoft")
            .Next(x => x.WithComparers(StringComparer.OrdinalIgnoreCase).AsOpResult());
}
