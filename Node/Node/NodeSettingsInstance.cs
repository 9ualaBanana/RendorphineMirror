namespace Node;

public class NodeSettingsInstance : IPlacedTasksStorage, IQueuedTasksStorage, ICompletedTasksStorage, IWatchingTasksStorage
{
    public DatabaseValueDictionary<string, ReceivedTask> QueuedTasks { get; }
    public DatabaseValueDictionary<string, WatchingTask> WatchingTasks { get; }
    public DatabaseValueDictionary<string, DbTaskFullState> PlacedTasks { get; }
    public DatabaseValueDictionary<string, CompletedTask> CompletedTasks { get; }

    public NodeSettingsInstance(DataDirs dirs)
    {
        QueuedTasks = new(new Database(dirs.DataFile("queued.db")), nameof(QueuedTasks), t => t.Id, serializer: JsonSettings.Default);
        WatchingTasks = new(new Database(dirs.DataFile("watching.db")), nameof(WatchingTasks), t => t.Id, serializer: JsonSettings.Default);
        PlacedTasks = new(new Database(dirs.DataFile("placed.db")), nameof(PlacedTasks), t => t.Id, serializer: JsonSettings.Default);
        CompletedTasks = new(new Database(dirs.DataFile("completed.db")), nameof(CompletedTasks), t => t.TaskInfo.Id, serializer: JsonSettings.Default);
    }
}
