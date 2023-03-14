namespace NodeUI;

public static class UICache
{
    public static readonly BindableDictionary<PluginType, SoftwareStats> SoftwareStats = new(ImmutableDictionary<PluginType, SoftwareStats>.Empty);
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
        var data = await Api.Default.ApiGet<ImmutableDictionary<PluginType, SoftwareStats>>($"{Api.TaskManagerEndpoint}/getsoftwarestats", "stats", "Getting software stats").ConfigureAwait(false);
        if (data) SoftwareStats.SetRange(data.Value);

        return data.GetResult();
    }
}
