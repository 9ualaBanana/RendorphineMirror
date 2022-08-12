using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Common.NodeToUI;

/// <summary> Node runtime state. Not being saved anywhere and is used for node -> ui communication </summary>
public class NodeGlobalState
{
    public static readonly NodeGlobalState Instance = new();

    [JsonIgnore]
    public readonly WeakEventManager<string> AnyChanged = new();

    public readonly Bindable<TasksFullDescriber> TaskDefinitions = new();
    public readonly BindableList<Plugin> InstalledPlugins = new();
    public readonly BindableDictionary<string, JToken?> ExecutingBenchmarks = new();
    public readonly BindableList<ReceivedTask> ExecutingTasks = new();
    public readonly BindableList<PlacedTask> PlacedTasks = new();
    public readonly BindableList<JObject> WatchingTasks = new();


    private NodeGlobalState()
    {
        TaskDefinitions.Changed += (_, _) => AnyChanged.Invoke(nameof(TaskDefinitions));
        InstalledPlugins.Changed += _ => AnyChanged.Invoke(nameof(InstalledPlugins));
        ExecutingBenchmarks.Changed += _ => AnyChanged.Invoke(nameof(ExecutingBenchmarks));
        ExecutingTasks.Changed += _ => AnyChanged.Invoke(nameof(ExecutingTasks));
        PlacedTasks.Changed += _ => AnyChanged.Invoke(nameof(PlacedTasks));
        WatchingTasks.Changed += _ => AnyChanged.Invoke(nameof(WatchingTasks));
    }
}
