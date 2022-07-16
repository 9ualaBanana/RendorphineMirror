using Newtonsoft.Json;

namespace NodeUI
{
    public static class GlobalState
    {
        public static readonly Bindable<INodeState> State = new(IdleNodeState.Instance);

        public static string GetStateName(this INodeState state) => state.GetType().Name[..^"NodeState".Length];
        public static void SubscribeStateChanged<T>(ChangedDelegate<INodeState> func) where T : INodeState
        {
            State.Changed += (oldstate, newstate) =>
            {
                if (oldstate.GetType() != typeof(T) && newstate.GetType() != typeof(T)) return;

                func(oldstate, newstate);
            };
        }


        public static readonly Bindable<ImmutableDictionary<PluginType, SoftwareStats>> SoftwareStats = new(ImmutableDictionary<PluginType, SoftwareStats>.Empty);
        static Thread? StatsUpdatingThread;
        static Timer? StatsUpdatingTimer;

        public static void StartUpdatingStats()
        {
            StatsUpdatingTimer?.Dispose();

            StatsUpdatingTimer = new Timer(
                _ => UpdateStatsAsync().ContinueWith(t => t.Result.LogIfError()),
                null, TimeSpan.Zero, TimeSpan.FromMinutes(1)
            );
        }
        static async Task<OperationResult> UpdateStatsAsync()
        {
            var data = await Apis.GetSoftwareStatsAsync().ConfigureAwait(false);
            if (data) SoftwareStats.Value = data.Value;

            return data.GetResult();
        }


        static readonly Bindable<TasksFullDescriber?> TasksInfo = new();
        static readonly string TasksInfoCacheFile = Path.Combine(Init.ConfigDirectory, "tasksinfocache");

        public static async ValueTask<TasksFullDescriber> GetTasksInfoAsync()
        {
            if (TasksInfo.Value is not null) return TasksInfo.Value;

            var data = await LocalApi.Send<TasksFullDescriber>("getactions").ConfigureAwait(false);
            if (data) await File.WriteAllTextAsync(TasksInfoCacheFile, JsonConvert.SerializeObject(data.Value, LocalApi.JsonSettingsWithType)).ConfigureAwait(false);

            if (!data && File.Exists(TasksInfoCacheFile))
                data = OperationResult.WrapException(() => JsonConvert.DeserializeObject<TasksFullDescriber>(File.ReadAllText(TasksInfoCacheFile), LocalApi.JsonSettingsWithType)!.AsOpResult());

            return TasksInfo.Value = data.ThrowIfError();
        }
    }
}