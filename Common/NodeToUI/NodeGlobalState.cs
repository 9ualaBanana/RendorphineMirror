using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Common.NodeToUI;

public class NodeGlobalState
{
    public static readonly NodeGlobalState Instance = new();

    [JsonIgnore]
    public readonly WeakEventManager AnyChanged = new();

    public readonly BindableDictionary<string, JObject?> ExecutingBenchmarks = new();
    public readonly BindableList<ReceivedTask> ExecutingTasks = new();


    private NodeGlobalState()
    {
        ExecutingBenchmarks.Changed += _ => AnyChanged.Invoke();
        ExecutingTasks.Changed += _ => AnyChanged.Invoke();
    }
}
