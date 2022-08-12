using static Common.Settings;

namespace Node;

public static class NodeSettings
{
    public static readonly DatabaseBindableList<ReceivedTask> SavedTasks;
    public static readonly DatabaseBindableList<WatchingTask> WatchingTasks;
    public static readonly DatabaseBindableList<PlacedTask> PlacedTasks;

    static NodeSettings()
    {
        SavedTasks = new(nameof(SavedTasks));
        WatchingTasks = new(nameof(WatchingTasks));
        PlacedTasks = new(nameof(PlacedTasks));
    }
}