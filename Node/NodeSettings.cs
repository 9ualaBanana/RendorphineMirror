using Newtonsoft.Json;
using static Common.Settings;

namespace Node;

public static class NodeSettings
{
    public static readonly DatabaseValueList<ReceivedTask> QueuedTasks;
    public static readonly DatabaseValueList<WatchingTask> WatchingTasks;
    public static readonly DatabaseValueList<DbTaskFullState> PlacedTasks;
    public static readonly DatabaseValueSplitDictionary<string, CompletedTask> CompletedTasks;

    static NodeSettings()
    {
        QueuedTasks = new(nameof(QueuedTasks));
        WatchingTasks = new(nameof(WatchingTasks));
        PlacedTasks = new("PlacedTasks2");
        CompletedTasks = new(nameof(CompletedTasks));

        WatchingTasks.Bindable.SubscribeChanged(() =>
            NodeGlobalState.Instance.WatchingTasks.SetRange(WatchingTasks.Bindable.Select(x => JsonConvert.DeserializeObject<WatchingTaskInfo>(JsonConvert.SerializeObject(x))!))
        , true);

        NodeGlobalState.Instance.PlacedTasks.Bind(PlacedTasks.Bindable);
        NodeGlobalState.Instance.QueuedTasks.Bind(QueuedTasks.Bindable);
    }
}