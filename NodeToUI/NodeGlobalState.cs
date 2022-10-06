using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NodeToUI;

/// <summary> Node runtime state. Not being saved anywhere and is used for node -> ui communication </summary>
public class NodeGlobalState
{
    public static readonly NodeGlobalState Instance = new();

    [JsonIgnore]
    public readonly WeakEventManager<string> AnyChanged = new();

    public readonly Bindable<TasksFullDescriber> TaskDefinitions = new();
    public readonly BindableList<Plugin> InstalledPlugins = new();
    public readonly BindableDictionary<string, JToken?> ExecutingBenchmarks = new();
    public readonly BindableList<ReceivedTask> QueuedTasks = new();
    public readonly BindableList<ReceivedTask> ExecutingTasks = new();
    public readonly BindableList<DbTaskFullState> PlacedTasks = new();
    public readonly BindableList<WatchingTask> WatchingTasks = new();


    private NodeGlobalState()
    {
        TaskDefinitions.Changed += () => AnyChanged.Invoke(nameof(TaskDefinitions));
        InstalledPlugins.Changed += () => AnyChanged.Invoke(nameof(InstalledPlugins));
        ExecutingBenchmarks.Changed += () => AnyChanged.Invoke(nameof(ExecutingBenchmarks));
        QueuedTasks.Changed += () => AnyChanged.Invoke(nameof(QueuedTasks));
        ExecutingTasks.Changed += () => AnyChanged.Invoke(nameof(ExecutingTasks));
        PlacedTasks.Changed += () => AnyChanged.Invoke(nameof(PlacedTasks));
        WatchingTasks.Changed += () => AnyChanged.Invoke(nameof(WatchingTasks));
    }


    public PluginType GetPluginTypeFromAction(string action) => TaskDefinitions.Value.Actions.First(x => x.Name == action).Type;
    public PluginType GetPluginType(ReceivedTask task) => GetPluginTypeFromAction(task.Info.TaskType);
    public Plugin GetPluginInstance(PluginType type) => InstalledPlugins.First(x => x.Type == type);
}
