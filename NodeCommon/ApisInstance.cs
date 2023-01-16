using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NodeCommon;

public record ApisInstance(HttpClient Client, string SessionId, bool LogErrors = true)
{
    const string TaskManagerEndpoint = Api.TaskManagerEndpoint;
    public static readonly ApisInstance Default = new(Api.Client, Settings.SessionId);


    public ApisInstance NoErrors() => this with { LogErrors = false };

    (string, string)[] AddSessionId(params (string, string)[] values)
    {
        if (values.Any(x => x.Item1 == "sessionid")) return values;
        return values.Append(("sessionid", SessionId)).ToArray();
    }

    public ValueTask<OperationResult> ShardPost(ITaskApi task, string url, string? property, string errorDetails, HttpContent content) =>
        ShardPost<JToken>(task, url, property, errorDetails, content).Next(j => true);
    public ValueTask<OperationResult<T>> ShardPost<T>(ITaskApi task, string url, string? property, string errorDetails, HttpContent content) =>
        ShardSend(task, url, url => Client.ApiPost<T>(url, property, errorDetails, content));

    public ValueTask<OperationResult> ShardGet(ITaskApi task, string url, string errorDetails, params (string, string)[] values) =>
        ShardGet(task, url, errorDetails, values);
    public ValueTask<OperationResult> ShardPost(ITaskApi task, string url, string errorDetails, params (string, string)[] values) =>
        ShardPost(task, url, errorDetails, values);
    public ValueTask<OperationResult<T>> ShardGet<T>(ITaskApi task, string url, string? property, string errorDetails, params (string, string)[] values) =>
        ShardGet<T>(task, url, property, errorDetails, values);
    public ValueTask<OperationResult<T>> ShardPost<T>(ITaskApi task, string url, string? property, string errorDetails, params (string, string)[] values) =>
        ShardPost<T>(task, url, property, errorDetails, values);

    async ValueTask<OperationResult<T>> ShardSend<T>(ITaskApi task, string url, Func<string, ValueTask<OperationResult<T>>> func)
    {
        (task as ILoggable)?.LogTrace($"Sending shard request {url}; Shard is {task.HostShard ?? "<null>"}");
        return await ShardSend(task, () => func($"https://{task.HostShard}/rphtasklauncher/{url}"), true);


        async ValueTask<OperationResult<T>> ShardSend(ITaskApi task, Func<ValueTask<OperationResult<T>>> func, bool tryDefaultShard)
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
                if (LogErrors) (task as ILoggable)?.LogErr($"Got error {httpdata.ToString().ReplaceLineEndings(" ")} in ({result.Message})");

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
    public ValueTask<OperationResult> UpdateTaskShardAsync(ITaskApi task) =>
        GetTaskShardAsync(task.Id)
            .Next(s =>
            {
                (task as ILoggable)?.LogTrace($"Shard was updated to {s}");
                task.HostShard = s;
                return true;
            });

    /// <summary> Get shard host for a task. Might take a long time to process. Should never return an error, but who knows... </summary>
    public async ValueTask<OperationResult<string>> GetTaskShardAsync(string taskid)
    {
        var shard = await Client.ApiGet<string>($"{TaskManagerEndpoint}/gettaskshard", "host", "Getting task shard", AddSessionId(("taskid", taskid)));
        if (!shard)
        {
            var httpdata = shard.GetResult().HttpData;
            if (httpdata is null) return shard;

            if (!httpdata.Value.IsSuccessStatusCode)
            {
                await Task.Delay(30_000);
                return await GetTaskShardAsync(taskid);
            }
        }

        // TODO: fix -72 check
        if (!shard && shard.Message!.Contains("-72 error code", StringComparison.Ordinal))
        {
            await Task.Delay(30_000);
            return await GetTaskShardAsync(taskid);
        }

        return shard;
    }


    public async ValueTask<OperationResult<ImmutableArray<string>>> GetShardListAsync() =>
        await Api.ApiGet<ImmutableArray<string>>($"{TaskManagerEndpoint}/getshardlist", "list", "Getting shards list", AddSessionId());
    public async ValueTask<OperationResult<Dictionary<string, string>>> GetTaskShardsAsync(IEnumerable<string> taskids)
    {
        var sel = async (IEnumerable<string> ids) => await Client.ApiGet<Dictionary<string, string>>($"{TaskManagerEndpoint}/gettaskshards", "hosts", "Getting tasks shards",
            AddSessionId(("taskids", JsonConvert.SerializeObject(ids))));

        return await taskids.Chunk(100)
            .Select(sel)
            .MergeDictResults();
    }


    /// <summary> Updates <see cref="task.HostShard"/> values. Does NOT guarantee that all tasks provided will have shards </summary>
    /// <returns> Tasks with successfully updated shards </returns>
    public async ValueTask<OperationResult<DbTaskFullState[]>> UpdateTaskShardsAsync(IEnumerable<DbTaskFullState> tasks) =>
        await UpdateTaskShardsAsync(tasks.ToDictionary(x => x.Id));
    /// <inheritdoc cref="UpdateTaskShardsAsync"/>
    public async ValueTask<OperationResult<DbTaskFullState[]>> UpdateTaskShardsAsync(IReadOnlyDictionary<string, DbTaskFullState> tasks)
    {
        if (tasks.Count == 0) return Array.Empty<DbTaskFullState>();
        return await GetTaskShardsAsync(tasks.Keys)
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

    /// <summary> Updates <see cref="task.HostShard"/> values. Does not return until all shards are fetched </summary>
    public async ValueTask<OperationResult> UpdateAllTaskShardsAsync(IEnumerable<DbTaskFullState> tasks)
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

            var shards = await Client.GetTaskShardsAsync(tasksdict.Keys);
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
    public async ValueTask<OperationResult<TMTasksStateInfo>> GetTasksOnShardAsync(string shardhost) =>
        await Client.ApiGet<TMTasksStateInfo>($"https://{shardhost}/rphtasklauncher/getmytasksinfo", null, "Getting shard tasks", AddSessionId());
    /// <inheritdoc cref="GetTasksOnShardAsync(string, string)"/>
    public async ValueTask<OperationResult<(TaskState state, TMTaskStateInfo info)[]>> GetTasksOnShardsAsync(IEnumerable<string> shards)
    {
        var result = await shards.Select(async shard => await GetTasksOnShardAsync(shard)).MergeResults();
        if (!result) return result.GetResult();

        var itasks = result.Value.SelectMany(x => x.Input).Select(x => (TaskState.Input, x));
        var atasks = result.Value.SelectMany(x => x.Active).Select(x => (TaskState.Active, x));
        var otasks = result.Value.SelectMany(x => x.Output).Select(x => (TaskState.Output, x));
        return itasks.Concat(atasks).Concat(otasks).ToArray();
    }
    public record TMTasksStateInfo(ImmutableArray<TMTaskStateInfo> Input, ImmutableArray<TMTaskStateInfo> Active, ImmutableArray<TMTaskStateInfo> Output, int QueueSize, double AvgWaitTime, string ScGuid);
    public record TMTaskStateInfo(string Id, double Progress);

    /// <returns> Finished, Failed and Canceled tasks </returns>
    public async ValueTask<OperationResult<Dictionary<string, TMOldTaskStateInfo>>> GetFinishedTasksStatesAsync(IEnumerable<string> taskids)
    {
        var sel = async (IEnumerable<string> ids) => await Client.ApiPost<Dictionary<string, TMOldTaskStateInfo>>($"{TaskManagerEndpoint}/gettasksstate", "tasks", "Getting finished tasks states",
            AddSessionId(("taskids", JsonConvert.SerializeObject(ids))));

        return await taskids.Chunk(100)
            .Select(sel)
            .MergeDictResults();
    }
    public record TMOldTaskStateInfo(string Id, TaskState State, ITaskOutputInfo? Output, string? ErrMsg);


    /// <returns> Task state or null if the task is Finished/Canceled/Failed </returns>
    public async ValueTask<OperationResult<ServerTaskState?>> GetTaskStateAsync(ITaskApi task)
    {
        var get = () => task.ShardGet<ServerTaskState>("getmytaskstate", null, $"Getting {task.Id} task state", AddSessionId(("taskid", task.Id)));
        bool exists(string errmsg) => !errmsg.Contains("There is no task with such ID", StringComparison.Ordinal) && !errmsg.Contains("No shard known for this task", StringComparison.Ordinal);


        // (completed in this case means finished, canceled or failed)
        // Completed tasks are not being stored on shards anymore, so if we get `There is no task with such ID` error, the task probably was completed
        // On this error, we refetch the task shard and retry the request on that shard. If the same error is returned, the task was completed and we return null

        var state = await get();
        if (!state && !exists(state.Message!))
        {
            var update = await task.UpdateTaskShardAsync();
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
    public async ValueTask<OperationResult<ServerTaskState>> GetTaskStateAsyncOrThrow(ITaskApi task) =>
        await GetTaskStateAsync(task)
            .Next(state => state.ThrowIfNull($"Task {task.Id} is already completed").AsOpResult());
    public record ServerTaskState(TaskState State, double Progress, ITaskOutputInfo Output, TaskTimes Times, TaskServer? Server = null);


    public ValueTask<OperationResult> FailTaskAsync(ITaskApi task, string errorMessage) => ChangeStateAsync(task, TaskState.Failed, errorMessage);
    public ValueTask<OperationResult> ChangeStateAsync(ITaskApi task, TaskState state) => ChangeStateAsync(task, state, null);
    async ValueTask<OperationResult> ChangeStateAsync(ITaskApi task, TaskState state, string? errorMessage)
    {
        (task as ILoggable)?.LogInfo($"Changing state to {state}");


        var data = AddSessionId(("taskid", task.Id), ("newstate", state.ToString().ToLowerInvariant()));
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
    public ValueTask<OperationResult> SendTaskProgressAsync(ReceivedTask task) =>
        task.ShardGet("mytaskprogress", "Sending task progress",
            AddSessionId(("taskid", task.Id), ("curstate", task.State.ToString().ToLowerInvariant()), ("progress", task.Progress.ToString())));



    /// <returns> ALL user tasks by states, might take a while </returns>
    public ValueTask<OperationResult<Dictionary<TaskState, List<ServerTaskFullState>>>> GetAllMyTasksAsync(TaskState[] states)
    {
        return GetShardListAsync()
            .Next(shards => OperationResult.WrapException(() => states.Select(async state => (state, (await next(shards, state, null)).ToList()).AsOpResult()).MergeDictResults()));


        async ValueTask<IEnumerable<ServerTaskFullState>> next(ImmutableArray<string> shards, TaskState state, string? afterId)
        {
            var tasks = await GetMyTasksAsync(shards, state, afterId).ThrowIfError();
            if (tasks.Count <= 1) return tasks;

            return tasks.Concat(await next(shards, state, tasks.Max(x => x.Id)));
        }
    }
    /// <returns> User tasks by state, up to 500 per state </returns>
    public ValueTask<OperationResult<List<ServerTaskFullState>>> GetMyTasksAsync(TaskState[] states, string? afterId = null) =>
        GetShardListAsync()
            .Next(shards => states.Select(async state => await GetMyTasksAsync(shards, state, afterId)).MergeArrResults());
    /// <returns> User tasks by state, up to 500 </returns>
    ValueTask<OperationResult<List<ServerTaskFullState>>> GetMyTasksAsync(TaskState state, string? afterId = null) =>
        GetShardListAsync().Next(shards => GetMyTasksAsync(shards, state, afterId));
    /// <inheritdoc cref="GetMyTasksAsync(TaskState, string?, string?)"/>
    async ValueTask<OperationResult<List<ServerTaskFullState>>> GetMyTasksAsync(IReadOnlyCollection<string> shards, TaskState state, string? afterId = null)
    {
        var getfunc = (string shard) => Api.ApiGet<List<ServerTaskFullState>>($"https://{shard}/rphtasklauncher/gettasklist", "list", "Getting task list",
            AddSessionId(("state", state.ToString().ToLowerInvariant()), ("afterid", afterId ?? string.Empty)));

        return await shards.Select(async shard => await getfunc(shard)).MergeArrResults();
    }
    public record ServerTaskFullState : DbTaskFullState, ILoggable
    {
        public TaskServer? Server { get; set; }

        [JsonConstructor]
        public ServerTaskFullState(string id, string originGuid, TaskPolicy launchPolicy, TaskObject @object, ITaskInputInfo input, ITaskOutputInfo output, JObject data)
            : base(id, new TaskInfo(@object, input, output, data, launchPolicy, originGuid)) { }
    }


    public ValueTask<OperationResult<ImmutableArray<NodeInfo>>> GetMyNodesAsync() =>
        Api.ApiGet<ImmutableArray<NodeInfo>>($"{TaskManagerEndpoint}/getmynodes", "nodes", "Getting my nodes", AddSessionId());
    public ValueTask<OperationResult<NodeInfo>> GetNodeAsync(string nodeid) =>
        GetMyNodesAsync().Next(nodes => nodes.FirstOrDefault(x => x.Id == nodeid)?.AsOpResult() ?? OperationResult.Err($"Node with such id ({nodeid}) was not found"));

    public ValueTask<OperationResult<ImmutableDictionary<string, SoftwareDefinition>>> GetSoftwareAsync() =>
        LocalApi.Send<ImmutableDictionary<string, SoftwareDefinition>>(Settings.RegistryUrl, "getsoft")
            .Next(x => x.WithComparers(StringComparer.OrdinalIgnoreCase).AsOpResult());
}
