using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodeToUI.Requests;

namespace NodeToUI;

/// <summary> Node runtime state. Not being saved anywhere and is used for node -> ui communication </summary>
public class NodeGlobalState
{
    public static readonly NodeGlobalState Instance = new();

    [JsonIgnore]
    public readonly WeakEventManager<string> AnyChanged = new();

    public string? SessionId => AuthInfo?.SessionId;

    public readonly Bindable<TasksFullDescriber> TaskDefinitions = new();
    public readonly BindableList<Plugin> InstalledPlugins = new();
    public readonly BindableDictionary<string, JToken?> ExecutingBenchmarks = new();
    public readonly BindableList<ReceivedTask> QueuedTasks = new();
    public readonly BindableList<ReceivedTask> ExecutingTasks = new();
    public readonly BindableList<DbTaskFullState> PlacedTasks = new();
    public readonly BindableList<WatchingTask> WatchingTasks = new();
    public readonly Bindable<JObject?> BenchmarkResult = new();
    public readonly Bindable<uint> TaskAutoDeletionDelayDays = new();

    public string ServerUrl => BServerUrl.Value;
    public ushort LocalListenPort => BLocalListenPort.Value;
    public ushort UPnpPort => BUPnpPort.Value;
    public ushort UPnpServerPort => BUPnpServerPort.Value;
    public ushort DhtPort => BDhtPort.Value;
    public ushort TorrentPort => BTorrentPort.Value;
    public string? NodeName => BNodeName.Value;
    public AuthInfo? AuthInfo => BAuthInfo.Value;

    public readonly Bindable<string> BServerUrl = new();
    public readonly Bindable<ushort> BLocalListenPort = new();
    public readonly Bindable<ushort> BUPnpPort = new();
    public readonly Bindable<ushort> BUPnpServerPort = new();
    public readonly Bindable<ushort> BDhtPort = new();
    public readonly Bindable<ushort> BTorrentPort = new();
    public readonly Bindable<string?> BNodeName = new();
    public readonly Bindable<AuthInfo?> BAuthInfo = new();

    // string = request guid
    public readonly BindableDictionary<string, GuiRequest> Requests = new();


    private NodeGlobalState()
    {
        GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
            .Where(x => x.FieldType.IsAssignableTo(typeof(IBindable)))
            .Select(x => ((IBindable) x.GetValue(this)!, x.Name))
            .ToList()
            .ForEach(x => x.Item1.Changed += () => AnyChanged.Invoke(x.Name));
    }


    public PluginType GetPluginTypeFromAction(string action) => TaskDefinitions.Value.Actions.First(x => x.Name == action).Type;
    public PluginType GetPluginType(ReceivedTask task) => GetPluginTypeFromAction(task.Info.TaskType);
    public Plugin GetPluginInstance(PluginType type) => InstalledPlugins.First(x => x.Type == type);
}
