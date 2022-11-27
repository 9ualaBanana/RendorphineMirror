using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NodeUI;

public static class UICache
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

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
        var data = await Api.ApiGet<ImmutableDictionary<PluginType, SoftwareStats>>($"{Api.TaskManagerEndpoint}/getsoftwarestats", "stats").ConfigureAwait(false);
        if (data) SoftwareStats.SetRange(data.Value);

        return data.GetResult();
    }

    public static readonly Bindable<bool> IsConnectedToNode = new();
    public static async Task StartUpdatingState(CancellationToken token = default)
    {
        var cachefile = Path.Combine(Init.ConfigDirectory, "nodeinfocache");

        if (Init.IsDebug)
        {
            NodeGlobalState.Instance.AnyChanged.Subscribe(NodeGlobalState.Instance, _ =>
                File.WriteAllText(cachefile, JsonConvert.SerializeObject(NodeGlobalState.Instance, LocalApi.JsonSettingsWithType)));
        }

        var cacheloaded = !Init.IsDebug; // dont load from cache is not debug
        var consecutive = 0;
        while (true)
        {
            try
            {
                var stream = await LocalPipe.SendLocalAsync("getstate").ConfigureAwait(false);
                var reader = LocalPipe.CreateReader(stream);
                consecutive = 0;
                IsConnectedToNode.Value = true;

                while (true)
                {
                    var read = reader.Read();
                    if (!read) break;
                    if (token.IsCancellationRequested) return;

                    var jtoken = JToken.Load(reader);
                    _logger.Debug($"Node state updated: {string.Join(", ", (jtoken as JObject)?.Properties().Select(x => x.Name) ?? new[] { jtoken.ToString(Formatting.None) })}");

                    using var tokenreader = jtoken.CreateReader();
                    LocalApi.JsonSerializerWithType.Populate(tokenreader, NodeGlobalState.Instance);
                }
            }
            catch (Exception ex)
            {
                IsConnectedToNode.Value = false;
                if (consecutive < 3) _logger.Error($"Could not read node state: {ex.Message}, reconnecting...");
                else if (consecutive == 3) _logger.Error($"Could not read node state after {consecutive} retries, disabling connection retry logging...");

                consecutive++;


                if (!cacheloaded)
                {
                    cacheloaded = true;

                    if (File.Exists(cachefile))
                    {
                        try { JsonConvert.PopulateObject(File.ReadAllText(cachefile), NodeGlobalState.Instance, LocalApi.JsonSettingsWithType); }
                        catch { }
                    }
                }
            }

            await Task.Delay(1_000).ConfigureAwait(false);
        }
    }
}
