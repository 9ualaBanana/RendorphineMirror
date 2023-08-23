using System.Diagnostics;

namespace NodeCommon;

public static class ApiExtensions
{
    const string RegistryUrl = Apis.RegistryUrl;
    static string TaskManagerEndpoint => Api.TaskManagerEndpoint;


    /// <summary> Updates <see cref="task.HostShard"/> values. Does NOT guarantee that all tasks provided will have shards </summary>
    /// <returns> Tasks with successfully updated shards </returns>
    public static async ValueTask<OperationResult<DbTaskFullState[]>> UpdateTaskShardsAsync(this Apis api, IEnumerable<DbTaskFullState> tasks) =>
        await api.UpdateTaskShardsAsync(tasks.ToDictionary(x => x.Id));
    /// <inheritdoc cref="UpdateTaskShardsAsync"/>
    public static async ValueTask<OperationResult<DbTaskFullState[]>> UpdateTaskShardsAsync(this Apis api, IReadOnlyDictionary<string, DbTaskFullState> tasks)
    {
        if (tasks.Count == 0) return Array.Empty<DbTaskFullState>();
        return await api.GetTaskShardsAsync(tasks.Keys)
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
    public static async ValueTask<OperationResult> UpdateAllTaskShardsAsync(this Apis api, IEnumerable<IRegisteredTaskApi> tasks)
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

            var shards = await api.GetTaskShardsAsync(tasksdict.Keys);
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
    public static async ValueTask<OperationResult<TMTasksStateInfo>> GetTasksOnShardAsync(this Apis api, string shardhost) =>
        await api.Api.ApiGet<TMTasksStateInfo>($"https://{shardhost}/rphtasklauncher/getmytasksinfo", null, "Getting shard tasks", api.AddSessionId());
    /// <inheritdoc cref="GetTasksOnShardAsync(string, string)"/>
    public static async ValueTask<OperationResult<ImmutableDictionary<TaskState, ImmutableArray<TMTaskStateInfo>>>> GetTasksOnShardsAsync(this Apis api, IEnumerable<string> shards)
    {
        var result = await shards.Select(async shard => await api.GetTasksOnShardAsync(shard)).MergeResults();
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
    public static async ValueTask<OperationResult<Dictionary<string, TMOldTaskStateInfo>>> GetFinishedTasksStatesAsync(this Apis api, IEnumerable<string> taskids)
    {
        var sel = async (IEnumerable<string> ids) => await api.Api.ApiPost<Dictionary<string, TMOldTaskStateInfo>>($"{TaskManagerEndpoint}/gettasksstate", "tasks", "Getting finished tasks states",
            api.AddSessionId(("taskids", JsonConvert.SerializeObject(ids))));

        return await taskids.Chunk(100)
            .Select(sel)
            .MergeDictResults();
    }


    /// <returns> Task state or null if the task is Finished/Canceled/Failed; without fetching shards </returns>
    public static async ValueTask<OperationResult<ServerTaskState?>> JustGetTaskStateAsync(this Apis api, IRegisteredTaskApi task)
    {
        var get = () => api.ShardGet<ServerTaskState>(task, "getmytaskstate", null, $"Getting {task.Id} task state", api.AddSessionId(("taskid", task.Id)));
        bool exists(string errmsg) => !errmsg.Contains("There is no task with such ID", StringComparison.Ordinal) && !errmsg.Contains("No shard known for this task", StringComparison.Ordinal);

        var state = await get();
        if (!state && !exists(state.Message!))
            return null as ServerTaskState;

        if (task is TaskBase rtask)
            rtask.SetStateTime(state.Value.State);
        return state!;
    }
    /// <returns> Task state or null if the task is Finished/Canceled/Failed </returns>
    public static async ValueTask<OperationResult<ServerTaskState?>> GetTaskStateAsync(this Apis api, IRegisteredTaskApi task)
    {
        var state = await api.JustGetTaskStateAsync(task);
        if (!state) return state;

        if (state.Value is null)
        {
            var update = await api.UpdateTaskShardAsync(task);
            if (!update) return update;

            state = await api.JustGetTaskStateAsync(task);
        }

        return state;
    }

    /// <returns> Task state; Throws if task is Finished/Canceled/Failed. </returns>
    public static async ValueTask<OperationResult<ServerTaskState>> GetTaskStateAsyncOrThrow(this Apis api, IRegisteredTaskApi task) =>
        await api.GetTaskStateAsync(task)
            .Next(state => state.ThrowIfNull($"Task {task.Id} is already completed").AsOpResult());


    public static ValueTask<OperationResult> FailTaskAsync(this Apis api, IRegisteredTaskApi task, string errmsg, string fullerrmsg) => api.ChangeStateAsync(task, TaskState.Failed, errmsg, fullerrmsg);
    public static ValueTask<OperationResult> ChangeStateAsync(this Apis api, IRegisteredTaskApi task, TaskState state) => api.ChangeStateAsync(task, state, null, null);
    async static ValueTask<OperationResult> ChangeStateAsync(this Apis api, IRegisteredTaskApi task, TaskState state, string? errmsg, string? fullerrmsg)
    {
        (task as ILoggable)?.LogInfo($"Changing state to {state}");


        var data = api.AddSessionId(("taskid", task.Id), ("newstate", state.ToString().ToLowerInvariant()));
        if (errmsg is not null)
            data = data.Append(("errormessage", errmsg)).ToArray();
        if (fullerrmsg is not null)
            data = data.Append(("fullerrormessage", fullerrmsg)).ToArray();

        var result = await api.ShardGet(task, "mytaskstatechanged", "Changing task state", data).ConfigureAwait(false);


        result.LogIfError("Error while changing task state: {0}", task as ILoggable);
        if (result && task is TaskBase rtask)
        {
            rtask.State = state;
            rtask.SetStateTime(state);
        }

        return result;
    }

    /// <summary> Send current task progress to the server </summary>
    public static ValueTask<OperationResult> SendTaskProgressAsync(this Apis api, IMPlusTask task) =>
        api.SendTaskProgressAsync(task, task.State, task.Progress);

    /// <summary> Send current task progress to the server </summary>
    public static ValueTask<OperationResult> SendTaskProgressAsync(this Apis api, IRegisteredTaskApi task, TaskState curstate, double progress) =>
        api.ShardGet(task, "mytaskprogress", "Sending task progress",
            api.AddSessionId(("taskid", task.Id), ("curstate", curstate.ToString().ToLowerInvariant()), ("progress", progress.ToString())));



    /// <returns> ALL user tasks by states, might take a while </returns>
    public static ValueTask<OperationResult<Dictionary<TaskState, List<ServerTaskFullState>>>> GetAllMyTasksAsync(this Apis api, TaskState[] states)
    {
        return api.GetShardListAsync()
            .Next(shards => OperationResult.WrapException(() => states.Select(async state => (state, (await next(shards, state, null)).ToList()).AsOpResult()).MergeDictResults()));


        async ValueTask<IEnumerable<ServerTaskFullState>> next(ImmutableArray<string> shards, TaskState state, string? afterId)
        {
            var tasks = await api.GetMyTasksAsync(shards, state, afterId).ThrowIfError();
            if (tasks.Count <= 1) return tasks;

            return tasks.Concat(await next(shards, state, tasks.Max(x => x.Id)));
        }
    }
    /// <returns> User tasks by state, up to 500 per state </returns>
    public static ValueTask<OperationResult<List<ServerTaskFullState>>> GetMyTasksAsync(this Apis api, TaskState[] states, string? afterId = null) =>
        api.GetShardListAsync()
            .Next(shards => states.Select(async state => await api.GetMyTasksAsync(shards, state, afterId)).MergeArrResults());
    /// <returns> User tasks by state, up to 500 </returns>
    static ValueTask<OperationResult<List<ServerTaskFullState>>> GetMyTasksAsync(this Apis api, TaskState state, string? afterId = null) =>
        api.GetShardListAsync().Next(shards => api.GetMyTasksAsync(shards, state, afterId));
    /// <inheritdoc cref="GetMyTasksAsync(TaskState, string?)"/>
    static async ValueTask<OperationResult<List<ServerTaskFullState>>> GetMyTasksAsync(this Apis api, IReadOnlyCollection<string> shards, TaskState state, string? afterId = null)
    {
        var getfunc = (string shard) => api.Api.ApiGet<List<ServerTaskFullState>>($"https://{shard}/rphtasklauncher/gettasklist", "list", "Getting task list",
            api.AddSessionId(("state", state.ToString().ToLowerInvariant()), ("afterid", afterId ?? string.Empty)));

        return await shards.Select(async shard => await getfunc(shard)).MergeArrResults();
    }


    public static ValueTask<OperationResult<ImmutableArray<NodeInfo>>> GetMyNodesAsync(this Apis api) =>
        api.Api.ApiGet<ImmutableArray<NodeInfo>>($"{TaskManagerEndpoint}/getmynodes", "nodes", "Getting my nodes", api.AddSessionId());
    public static ValueTask<OperationResult<NodeInfo>> GetNodeAsync(this Apis api, string nodeid) =>
        api.GetMyNodesAsync().Next(nodes => nodes.FirstOrDefault(x => x.Id == nodeid)?.AsOpResult() ?? OperationResult.Err($"Node with such id ({nodeid}) was not found"));

    public static ValueTask<OperationResult<ImmutableDictionary<string, SoftwareDefinition>>> GetSoftwareAsync(this Apis api) =>
        api.Api.ApiGet<ImmutableDictionary<string, SoftwareDefinition>>($"{RegistryUrl}/getsoft", "value", "Getting registry software")
            .Next(x => x.WithComparers(StringComparer.OrdinalIgnoreCase).AsOpResult());

    public static async ValueTask<OperationResult<string>> GetMPlusItemDownloadLinkAsync(
        this Apis api,
        IRegisteredTaskApi registeredTaskApi,
        string iid,
        Extension extension)
        => await api.ShardGet<string>(registeredTaskApi, "getmplusitemdownloadlink", "link", "Getting M+ item download link",
            api.AddSessionId(("iid", iid), ("format", extension.ToString().ToLower()), ("original", extension == Extension.jpeg ? "1" : "0")));


    public static ValueTask<OperationResult<UUserSettings>> GetSettingsAsync(this Apis api) =>
        api.Api.ApiGet<ServerUserSettings>($"{TaskManagerEndpoint}/getmysettings", "settings", "Getting user settings", api.AddSessionId())
            .Next(settings => settings.ToSettings().AsOpResult());

    public static ValueTask<OperationResult> SetSettingsAsync(this Apis api, UUserSettings userSettings) =>
        api.Api.ApiPost($"{TaskManagerEndpoint}/setusersettings", "Setting user settings",
            api.AddSessionId(("settings", JsonConvert.SerializeObject(ServerUserSettings.FromSettings(userSettings), JsonSettings.LowercaseIgnoreNull)))
        );


    /*
    Actual TMServerSoftware schema:
    {
        [plugin_name]: {
            [plugin_version]: {
                plugins: {
                    [subplugin_name]: {
                        version: [subplugin_version],
                        subplugins: {
                            [subsubplugin_name]: [subsubplugin_version],
                        }
                    }
                }
            }
        }
    }

    But since this is stupid, and the server doesn't validate software names, we can use a more flattened schema:
    (The server still expects there to be a "plugins" object on every software version, so we need to keep it)
    {
        [plugin_name]: {
            [plugin_version]: { "plugins": {} }
        }
    }
    */
    public class ServerUserSettings
    {
        [JsonProperty("nodeinstallsoftware")] public RNodeInstallSoftware? NodeInstallSoftware { get; private set; }
        [JsonProperty("installsoftware")] public TMServerSoftware? InstallSoftware { get; private set; }

        public ServerUserSettings(RNodeInstallSoftware? nodeInstallSoftware, TMServerSoftware? installSoftware)
        {
            NodeInstallSoftware = nodeInstallSoftware;
            InstallSoftware = installSoftware;
        }

        public UUserSettings ToSettings()
        {
            var soft = toSoftware(InstallSoftware);

            var nodesoft = new UUserSettings.RNodeInstallSoftware();
            if (NodeInstallSoftware is not null)
                foreach (var (node, isoft) in NodeInstallSoftware)
                    nodesoft.Add(node, toSoftware(isoft));

            return new UUserSettings(nodesoft, soft);


            static UUserSettings.TMServerSoftware toSoftware(TMServerSoftware? software)
            {
                var result = new UUserSettings.TMServerSoftware();

                if (software is not null)
                    foreach (var (type, versions) in software)
                    {
                        if (!Enum.TryParse<PluginType>(type, true, out var plugintype))
                        {
                            // TODO: log instead of throw
                            throw new Exception($"Unknown plugin type {type}");
                            continue;
                        }

                        result.Add(plugintype, versions.Keys.ToHashSet());
                    }

                return result;
            }
        }
        public static ServerUserSettings FromSettings(UUserSettings settings)
        {
            var soft = toSoftware(settings.InstallSoftware);

            var nodesoft = new RNodeInstallSoftware();
            foreach (var (node, isoft) in settings.NodeInstallSoftware)
                nodesoft.Add(node, toSoftware(isoft));

            return new ServerUserSettings(nodesoft, soft);


            static TMServerSoftware toSoftware(UUserSettings.TMServerSoftware software)
            {
                var result = new TMServerSoftware();

                if (software is not null)
                    foreach (var (type, versions) in software)
                    {
                        var softversions = new TMServerSoftwareVersions();
                        foreach (var version in versions)
                            softversions.Add(version, new UserSettingsSoft());

                        result.Add(type.ToString().ToLowerInvariant(), softversions);
                    }

                return result;
            }
        }


        public class RNodeInstallSoftware : Dictionary<string, TMServerSoftware> { }        // <node GUID, ...>
        public class TMServerSoftware : Dictionary<string, TMServerSoftwareVersions> { }    // <plugin, ...>
        public class TMServerSoftwareVersions : Dictionary<PluginVersion, UserSettingsSoft> { }    // <version, ...>
        public class UserSettingsSoft
        {
            [JsonProperty("plugins")] public readonly object Plugins = new();
        }
    }
}