using static Common.Settings;

namespace Node;

public static class NodeSettings
{
    public static DatabaseValueList<ReceivedTask> SavedTasks;
    public static DatabaseValueList<WatchingTask> WatchingTasks;
    public static DatabaseValueList<PlacedTask> PlacedTasks;

    static NodeSettings()
    {
        SavedTasks = new(nameof(SavedTasks));
        WatchingTasks = new(nameof(WatchingTasks));
        PlacedTasks = new(nameof(PlacedTasks));

        WatchingTasks.Bindable.SubscribeChanged(() => NodeGlobalState.Instance.WatchingTasks.SetRange(WatchingTasks.Bindable.Select(x => x.AsInfo())), true);
        NodeGlobalState.Instance.PlacedTasks.Bind(PlacedTasks.Bindable);
    }
}