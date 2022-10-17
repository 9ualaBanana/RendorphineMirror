using static Common.Settings;

namespace Node;

public static class NodeSettings
{
    public static readonly DatabaseValueDictionary<string, ReceivedTask> QueuedTasks;
    public static readonly DatabaseValueDictionary<string, WatchingTask> WatchingTasks;
    public static readonly DatabaseValueDictionary<string, DbTaskFullState> PlacedTasks;
    public static readonly DatabaseValueDictionary<string, CompletedTask> CompletedTasks;

    static NodeSettings()
    {
        QueuedTasks = new(nameof(QueuedTasks), t => t.Id);
        WatchingTasks = new(nameof(WatchingTasks), t => t.Id);
        PlacedTasks = new(nameof(PlacedTasks), t => t.Id);
        CompletedTasks = new(nameof(CompletedTasks), t => t.TaskInfo.Id);

        NodeGlobalState.Instance.WatchingTasks.Bind(WatchingTasks.Bindable);
        NodeGlobalState.Instance.PlacedTasks.Bind(PlacedTasks.Bindable);
        NodeGlobalState.Instance.QueuedTasks.Bind(QueuedTasks.Bindable);
    }
}