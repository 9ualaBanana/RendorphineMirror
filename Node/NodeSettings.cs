using Node.Tasks.Repeating;
using static Common.Settings;

namespace Node;

public static class NodeSettings
{
    public static readonly DatabaseBindableList<ReceivedTask> SavedTasks;
    public static readonly DatabaseBindableList<RepeatingTask> RepeatingTasks;
    public static readonly DatabaseBindableList<TaskCreationInfo> PlacedTasks;

    static NodeSettings()
    {
        SavedTasks = new(nameof(SavedTasks));
        RepeatingTasks = new(nameof(RepeatingTasks));
        PlacedTasks = new(nameof(PlacedTasks));
    }
}
