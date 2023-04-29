using System.Diagnostics;
using Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodeCommon.NodeUserSettings;

namespace NodeCommon;

public partial record Apis(ApiInstance Api, string SessionId, bool LogErrors = true)
{
    public const string RegistryUrl = "https://t.microstock.plus:7897";
    const string TaskManagerEndpoint = Common.Api.TaskManagerEndpoint;


    public static Apis DefaultWithSessionId(string sid) => new(Common.Api.Default, sid);
    public Apis WithSessionId(string sid) => this with { SessionId = sid };
    public Apis WithNoErrorLog() => this with { LogErrors = false };

    (string, string)[] AddSessionId(params (string, string)[] values)
    {
        if (values.Any(x => x.Item1 == "sessionid")) return values;
        return values.Append(("sessionid", SessionId)).ToArray();
    }

    public ValueTask<OperationResult> ShardPost(IRegisteredTaskApi task, string url, string? property, string errorDetails, HttpContent content) =>
        ShardPost<JToken>(task, url, property, errorDetails, content).Next(j => true);
    public ValueTask<OperationResult<T>> ShardPost<T>(IRegisteredTaskApi task, string url, string? property, string errorDetails, HttpContent content) =>
        ShardSend(task, url, url => Api.ApiPost<T>(url, property, errorDetails, content));

    public ValueTask<OperationResult> ShardGet(IRegisteredTaskApi task, string url, string errorDetails, params (string, string)[] values) =>
        ShardGet<JToken>(task, url, null, errorDetails, values).Next(j => true);
    public ValueTask<OperationResult> ShardPost(IRegisteredTaskApi task, string url, string errorDetails, params (string, string)[] values) =>
        ShardPost<JToken>(task, url, null, errorDetails, values).Next(j => true);
    public ValueTask<OperationResult<T>> ShardGet<T>(IRegisteredTaskApi task, string url, string? property, string errorDetails, params (string, string)[] values) =>
        ShardSend(task, url, url => Api.ApiGet<T>(url, property, errorDetails, AddSessionId(values)));
    public ValueTask<OperationResult<T>> ShardPost<T>(IRegisteredTaskApi task, string url, string? property, string errorDetails, params (string, string)[] values) =>
        ShardSend(task, url, url => Api.ApiPost<T>(url, property, errorDetails, Api.ToContent(AddSessionId(values))));

    async ValueTask<OperationResult<T>> ShardSend<T>(IRegisteredTaskApi task, string url, Func<string, ValueTask<OperationResult<T>>> func)
    {
        (task as ILoggable)?.LogTrace($"Sending shard request {url}; Shard is {task.HostShard ?? "<null>"}");
        return await ShardSend(task, () => func($"https://{task.HostShard}/rphtasklauncher/{url}"), true);


        async ValueTask<OperationResult<T>> ShardSend(IRegisteredTaskApi task, Func<ValueTask<OperationResult<T>>> func, bool tryDefaultShard)
        {
            if (tryDefaultShard)
            {
                task.HostShard ??= "tasks.microstock.plus";
                tryDefaultShard = false;
            }

            if (task.HostShard is null)
            {
                var host = await UpdateTaskShardAsync(task);
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
                    await UpdateTaskShardAsync(task);
                    return await ShardSend(task, func, tryDefaultShard);
                }

                // "No shard is known for this task. The shard could be restarting, try again in 30 seconds"
                if (httpdata.ErrorCode == ErrorCodes.Error && result.Message!.Contains("shard", StringComparison.OrdinalIgnoreCase))
                {
                    var host = await UpdateTaskShardAsync(task);
                    if (!host) return host;

                    await Task.Delay(30_000);
                    return await ShardSend(task, func, tryDefaultShard);
                }
            }

            return result;
        }
    }


    /// <inheritdoc cref="GetTaskShardAsync"/>
    public ValueTask<OperationResult> UpdateTaskShardAsync(IRegisteredTaskApi task) =>
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
        var shard = await Api.ApiGet<string>($"{TaskManagerEndpoint}/gettaskshard", "host", $"Getting {taskid} task shard", AddSessionId(("taskid", taskid)));
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

    /// <summary> Maybe get shard host for a task </summary>
    public async ValueTask<OperationResult<string?>> MaybeGetTaskShardAsync(string taskid)
    {
        var shard = await Api.ApiGet<string>($"{TaskManagerEndpoint}/gettaskshard", "host", $"Getting {taskid} task shard", AddSessionId(("taskid", taskid)));

        // TODO: fix -72 check
        if (!shard && shard.Message!.Contains("-72 error code", StringComparison.Ordinal))
            return null as string;

        return shard!;
    }


    public async ValueTask<OperationResult<ImmutableArray<string>>> GetShardListAsync() =>
        await Api.ApiGet<ImmutableArray<string>>($"{TaskManagerEndpoint}/getshardlist", "list", "Getting shards list", AddSessionId());
    public async ValueTask<OperationResult<Dictionary<string, string>>> GetTaskShardsAsync(IEnumerable<string> taskids)
    {
        var sel = async (IEnumerable<string> ids) => await Api.ApiGet<Dictionary<string, string>>($"{TaskManagerEndpoint}/gettaskshards", "hosts", "Getting tasks shards",
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
    public async ValueTask<OperationResult> UpdateAllTaskShardsAsync(IEnumerable<IRegisteredTaskApi> tasks)
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

            var shards = await GetTaskShardsAsync(tasksdict.Keys);
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
        await Api.ApiGet<TMTasksStateInfo>($"https://{shardhost}/rphtasklauncher/getmytasksinfo", null, "Getting shard tasks", AddSessionId());
    /// <inheritdoc cref="GetTasksOnShardAsync(string, string)"/>
    public async ValueTask<OperationResult<ImmutableDictionary<TaskState, ImmutableArray<TMTaskStateInfo>>>> GetTasksOnShardsAsync(IEnumerable<string> shards)
    {
        var result = await shards.Select(async shard => await GetTasksOnShardAsync(shard)).MergeResults();
        if (!result) return result.GetResult();

        return new Dictionary<TaskState, ImmutableArray<TMTaskStateInfo>>()
        {
            [TaskState.Input] = result.Value.SelectMany(x => x.Input).ToImmutableArray(),
            [TaskState.Active] = result.Value.SelectMany(x => x.Active).ToImmutableArray(),
            [TaskState.Output] = result.Value.SelectMany(x => x.Output).ToImmutableArray(),
            [TaskState.Validation] = result.Value.SelectMany(x => x.Validation).ToImmutableArray(),
        }.ToImmutableDictionary();
    }

    /// <returns> Finished, Failed and Canceled tasks </returns>
    public async ValueTask<OperationResult<Dictionary<string, TMOldTaskStateInfo>>> GetFinishedTasksStatesAsync(IEnumerable<string> taskids)
    {
        var sel = async (IEnumerable<string> ids) => await Api.ApiPost<Dictionary<string, TMOldTaskStateInfo>>($"{TaskManagerEndpoint}/gettasksstate", "tasks", "Getting finished tasks states",
            AddSessionId(("taskids", JsonConvert.SerializeObject(ids))));

        return await taskids.Chunk(100)
            .Select(sel)
            .MergeDictResults();
    }


    /// <returns> Task state or null if the task is Finished/Canceled/Failed; without fetching shards </returns>
    public async ValueTask<OperationResult<ServerTaskState?>> JustGetTaskStateAsync(IRegisteredTaskApi task)
    {
        var get = () => ShardGet<ServerTaskState>(task, "getmytaskstate", null, $"Getting {task.Id} task state", AddSessionId(("taskid", task.Id)));
        bool exists(string errmsg) => !errmsg.Contains("There is no task with such ID", StringComparison.Ordinal) && !errmsg.Contains("No shard known for this task", StringComparison.Ordinal);

        var state = await get();
        if (!state && !exists(state.Message!))
            return null as ServerTaskState;

        if (task is TaskBase rtask)
            rtask.SetStateTime(state.Value.State);
        return state!;
    }
    /// <returns> Task state or null if the task is Finished/Canceled/Failed </returns>
    public async ValueTask<OperationResult<ServerTaskState?>> GetTaskStateAsync(IRegisteredTaskApi task)
    {
        var state = await JustGetTaskStateAsync(task);
        if (!state) return state;

        if (state.Value is null)
        {
            var update = await UpdateTaskShardAsync(task);
            if (!update) return update;

            state = await JustGetTaskStateAsync(task);
        }

        return state;
    }

    /// <returns> Task state; Throws if task is Finished/Canceled/Failed. </returns>
    public async ValueTask<OperationResult<ServerTaskState>> GetTaskStateAsyncOrThrow(IRegisteredTaskApi task) =>
        await GetTaskStateAsync(task)
            .Next(state => state.ThrowIfNull($"Task {task.Id} is already completed").AsOpResult());


    public ValueTask<OperationResult> FailTaskAsync(IRegisteredTaskApi task, string errorMessage) => ChangeStateAsync(task, TaskState.Failed, errorMessage);
    public ValueTask<OperationResult> ChangeStateAsync(IRegisteredTaskApi task, TaskState state) => ChangeStateAsync(task, state, null);
    async ValueTask<OperationResult> ChangeStateAsync(IRegisteredTaskApi task, TaskState state, string? errorMessage)
    {
        (task as ILoggable)?.LogInfo($"Changing state to {state}");


        var data = AddSessionId(("taskid", task.Id), ("newstate", state.ToString().ToLowerInvariant()));
        if (errorMessage is not null)
        {
            if (state != TaskState.Failed)
                throw new ArgumentException($"Could not provide {nameof(errorMessage)} for task state {state}");

            data = data.Append(("errormessage", errorMessage)).ToArray();
        }

        var result = await ShardGet(task, "mytaskstatechanged", "Changing task state", data).ConfigureAwait(false);


        result.LogIfError("Error while changing task state: {0}", task);
        if (result && task is TaskBase rtask)
        {
            rtask.State = state;
            rtask.SetStateTime(state);
        }

        return result;
    }

    /// <summary> Send current task progress to the server </summary>
    public ValueTask<OperationResult> SendTaskProgressAsync(TaskBase task) =>
        ShardGet(task, "mytaskprogress", "Sending task progress",
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
    /// <inheritdoc cref="GetMyTasksAsync(TaskState, string?)"/>
    async ValueTask<OperationResult<List<ServerTaskFullState>>> GetMyTasksAsync(IReadOnlyCollection<string> shards, TaskState state, string? afterId = null)
    {
        var getfunc = (string shard) => Api.ApiGet<List<ServerTaskFullState>>($"https://{shard}/rphtasklauncher/gettasklist", "list", "Getting task list",
            AddSessionId(("state", state.ToString().ToLowerInvariant()), ("afterid", afterId ?? string.Empty)));

        return await shards.Select(async shard => await getfunc(shard)).MergeArrResults();
    }


    public ValueTask<OperationResult<ImmutableArray<NodeInfo>>> GetMyNodesAsync() =>
        Api.ApiGet<ImmutableArray<NodeInfo>>($"{TaskManagerEndpoint}/getmynodes", "nodes", "Getting my nodes", AddSessionId());
    public ValueTask<OperationResult<NodeInfo>> GetNodeAsync(string nodeid) =>
        GetMyNodesAsync().Next(nodes => nodes.FirstOrDefault(x => x.Id == nodeid)?.AsOpResult() ?? OperationResult.Err($"Node with such id ({nodeid}) was not found"));

    public ValueTask<OperationResult<ImmutableDictionary<string, SoftwareDefinition>>> GetSoftwareAsync() =>
        Api.ApiGet<ImmutableDictionary<string, SoftwareDefinition>>($"{RegistryUrl}/getsoft", "value", "Getting registry software")
            .Next(x => x.WithComparers(StringComparer.OrdinalIgnoreCase).AsOpResult());

    public async ValueTask<OperationResult<string>> GetMPlusItemDownloadLinkAsync(
        IRegisteredTaskApi registeredTaskApi,
        string iid,
        Extension extension)
        => await ShardGet<string>(registeredTaskApi, "getmplusitemdownloadlink", "link", "Getting M+ item download link",
            AddSessionId(("iid", iid), ("format", extension.ToString().ToLower()), ("original", extension == Extension.jpeg ? "1" : "0")));


    public ValueTask<OperationResult<UserSettings>> GetSettingsAsync() =>
        Api.ApiGet<UserSettings>($"{TaskManagerEndpoint}/getmysettings", "settings", "Getting user settings", AddSessionId());

    public ValueTask<OperationResult> SetSettingsAsync(UserSettings userSettings) =>
        Api.ApiPost($"{TaskManagerEndpoint}/setusersettings", "Setting user settings", AddSessionId(("settings", JsonConvert.SerializeObject(userSettings))));


    public ValueTask<OperationResult<UserSettings2>> GetSettings2Async() =>
        Api.ApiGet<UserSettings2>($"{TaskManagerEndpoint}/getmysettings", "settings", "Getting user settings", AddSessionId());
    public ValueTask<OperationResult> SetSettingsAsync(UserSettings2 userSettings) =>
        Api.ApiPost($"{TaskManagerEndpoint}/setusersettings", "Setting user settings", AddSessionId(("settings", JsonConvert.SerializeObject(userSettings, JsonSettings.LowercaseIgnoreNull))));
}