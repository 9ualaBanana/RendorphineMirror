using Newtonsoft.Json;

namespace NodeUI;

public static class UICache
{
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
