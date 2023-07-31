namespace Node;

public class NodeSettingsInstance : IPlacedTasksStorage, IQueuedTasksStorage, ICompletedTasksStorage, IWatchingTasksStorage
{
    public DatabaseValueDictionary<string, DbTaskFullState> PlacedTasks => NodeSettings.PlacedTasks;
    public DatabaseValueDictionary<string, ReceivedTask> QueuedTasks => NodeSettings.QueuedTasks;
    public DatabaseValueDictionary<string, CompletedTask> CompletedTasks => NodeSettings.CompletedTasks;
    public DatabaseValueDictionary<string, WatchingTask> WatchingTasks => NodeSettings.WatchingTasks;
}
