using static Common.Settings;

namespace Node;

public static class NodeSettings
{
    public static readonly DatabaseBindableList<ReceivedTask> ActiveTasks;

    static NodeSettings()
    {
        ActiveTasks = new(nameof(ActiveTasks));
    }
}
