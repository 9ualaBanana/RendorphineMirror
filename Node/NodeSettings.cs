using static Common.Settings;

namespace Node;

public static class NodeSettings
{
    public static readonly DatabaseBindableList<ReceivedTask> SavedTasks;
    public static readonly DatabaseBindableList<TaskCreationInfo> PlacedTasks;

    static NodeSettings()
    {
        SavedTasks = new(nameof(SavedTasks));
        PlacedTasks = new(nameof(PlacedTasks));
    }
}
