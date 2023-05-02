namespace Node;

public static class NodeSettings
{
    public static readonly DatabaseValueDictionary<string, ReceivedTask> QueuedTasks;
    public static readonly DatabaseValueDictionary<string, WatchingTask> WatchingTasks;
    public static readonly DatabaseValueDictionary<string, DbTaskFullState> PlacedTasks;
    public static readonly DatabaseValueDictionary<string, CompletedTask> CompletedTasks;

    static NodeSettings()
    {
        QueuedTasks = new(new Database(Path.Combine(Directories.Data, "queued.db")), nameof(QueuedTasks), t => t.Id, serializer: JsonSettings.Default);
        WatchingTasks = new(new Database(Path.Combine(Directories.Data, "watching.db")), nameof(WatchingTasks), t => t.Id, serializer: JsonSettings.Default);
        PlacedTasks = new(new Database(Path.Combine(Directories.Data, "placed.db")), nameof(PlacedTasks), t => t.Id, serializer: JsonSettings.Default);
        CompletedTasks = new(new Database(Path.Combine(Directories.Data, "completed.db")), nameof(CompletedTasks), t => t.TaskInfo.Id, serializer: JsonSettings.Default);
    }
}