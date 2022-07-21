using static Common.Settings;

namespace Node;

public static class NodeSettings
{
    public static readonly DatabaseBindableList<ReceivedTask> ExecutingTasks;
    public static readonly DatabaseBindableList<TaskCreationInfo> PlacedTasks;

    static NodeSettings()
    {
        ExecutingTasks = new(nameof(ExecutingTasks));
        PlacedTasks = new(nameof(PlacedTasks));
    }
}
