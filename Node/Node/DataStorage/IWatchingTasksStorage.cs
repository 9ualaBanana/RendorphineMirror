namespace Node.DataStorage;

public interface IWatchingTasksStorage
{
    DatabaseValueDictionary<string, WatchingTask> WatchingTasks { get; }
}
