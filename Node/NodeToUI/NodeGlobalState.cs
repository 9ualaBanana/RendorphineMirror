namespace NodeToUI;

/// <summary> Node runtime state. Not being saved anywhere and is used for node -> ui communication </summary>
public class NodeGlobalState
{
    public static readonly NodeGlobalState Instance = new();

    [JsonIgnore]
    public readonly WeakEventManager<string> AnyChanged = new();

    public readonly Bindable<TasksFullDescriber> TaskDefinitions = new();
    public readonly Bindable<ImmutableDictionary<PluginType, ImmutableDictionary<PluginVersion, SoftwareVersionInfo>>> Software = new(ImmutableDictionary<PluginType, ImmutableDictionary<PluginVersion, SoftwareVersionInfo>>.Empty);
    public readonly Bindable<ImmutableDictionary<string, SoftwareStats>> SoftwareStats = new(ImmutableDictionary<string, SoftwareStats>.Empty);
    public readonly Bindable<UUserSettings> UserSettings = new(new(null, null));
    public readonly Bindable<UserBalance> Balance = new();

    public readonly BindableList<Plugin> InstalledPlugins = new();
    public readonly BindableDictionary<string, JToken?> ExecutingBenchmarks = new();
    public readonly BindableList<ReceivedTask> QueuedTasks = new();
    public readonly BindableList<ReceivedTask> ExecutingTasks = new();
    public readonly BindableList<DbTaskFullState> PlacedTasks = new();
    public readonly BindableList<CompletedTask> CompletedTasks = new();
    public readonly BindableList<WatchingTask> WatchingTasks = new();
    public readonly BindableList<RFProduct> RFProducts = new();
    public readonly Bindable<JObject?> BenchmarkResult = new();
    public readonly Bindable<uint> TaskAutoDeletionDelayDays = new();

    public readonly Bindable<string> ServerUrl = new();
    public readonly Bindable<ushort> LocalListenPort = new();
    public readonly Bindable<ushort> UPnpPort = new();
    public readonly Bindable<ushort> UPnpServerPort = new();
    public readonly Bindable<ushort> DhtPort = new();
    public readonly Bindable<ushort> TorrentPort = new();
    public readonly Bindable<string?> NodeName = new();
    public readonly Bindable<AuthInfo?> AuthInfo = new();
    public readonly Bindable<bool> AcceptTasks = new();

    // string = request guid
    public readonly BindableDictionary<string, GuiRequest> Requests = new();


    private NodeGlobalState()
    {
        RFProducts.SubscribeChanged(() => Console.WriteLine("SUBSCRIBE CHANGED WOW " + RFProducts.Count));

        GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
            .Where(x => x.FieldType.IsAssignableTo(typeof(IBindable)))
            .Select(x => ((IBindable) x.GetValue(this)!, x.Name))
            .ToList()
            .ForEach(x => x.Item1.Changed += () => AnyChanged.Invoke(x.Name));
    }


    public IEnumerable<Plugin> GetPluginInstances(PluginType type) => InstalledPlugins.Where(x => x.Type == type);
    public Plugin GetPluginInstance(PluginType type) => GetPluginInstances(type).OrderByDescending(p => p.Version).First();
    public Plugin GetPluginInstance(PluginType type, PluginVersion version) => GetPluginInstances(type).First(x => x.Version == version);
}
