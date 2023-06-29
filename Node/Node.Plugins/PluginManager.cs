namespace Node.Plugins;

// TODO: invalidate every day or something
/// <summary> Stores and updates a list of installed plugins using provided discoverers </summary>
public class PluginManager : IInstalledPluginsProvider
{
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public IReadOnlyCollection<Plugin> Plugins => CachedPlugins.Value ?? GetInstalledPluginsAsync().GetAwaiter().GetResult();

    public IReadOnlyBindable<IReadOnlyCollection<Plugin>?> CachedPluginsBindable => CachedPlugins;
    readonly Bindable<IReadOnlyCollection<Plugin>?> CachedPlugins = new();

    readonly ImmutableArray<IPluginDiscoverer> Discoverers = new();
    TaskCompletionSource<IReadOnlyCollection<Plugin>>? CurrentDiscover;

    public PluginManager(IEnumerable<IPluginDiscoverer> discoverers) : this(discoverers.ToImmutableArray()) { }
    public PluginManager(ImmutableArray<IPluginDiscoverer> discoverers) => Discoverers = discoverers;


    /// <summary> Discovers installed plugins. Returns already cached result if available. </summary>
    public async Task<IReadOnlyCollection<Plugin>> GetInstalledPluginsAsync() => CachedPlugins.Value ?? await RediscoverPluginsAsync();

    /// <summary> Discovers installed plugins and caches them in <see cref="CachedPlugins"/> </summary>
    public async Task<IReadOnlyCollection<Plugin>> RediscoverPluginsAsync()
    {
        if (CurrentDiscover?.Task is { } task)
            return await task;

        CurrentDiscover = new();
        using var _ = new FuncDispose(() => CurrentDiscover = null);

        var plugins = (await Task.WhenAll(Discoverers.Select(discover))).SelectMany(p => p).ToArray();
        Logger.Info($"List of installed plugins was updated ({plugins.Length}):{Environment.NewLine}{string.Join(Environment.NewLine, plugins.Select(pluginToString))}");

        CachedPlugins.Value = plugins;
        CurrentDiscover.SetResult(plugins);
        return plugins;


        static string pluginToString(Plugin plugin) => $"  {plugin.Type} {plugin.Version}: {(plugin.Path.Length == 0 ? "<nopath>" : Path.GetFullPath(plugin.Path))}";
        static async Task<IEnumerable<Plugin>> discover(IPluginDiscoverer discoverer)
        {
            try
            {
                return await discoverer.DiscoverAsync();
            }
            catch (Exception ex)
            {
                Logger.Error($"Could not discover plugins using {discoverer.GetType().Name}: {ex}");
                return Enumerable.Empty<Plugin>();
            }
        }
    }
}
