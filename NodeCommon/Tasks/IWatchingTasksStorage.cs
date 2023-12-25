namespace NodeCommon.Tasks;

public interface IWatchingTasksStorage
{
    DatabaseValueDictionary<string, WatchingTask> WatchingTasks { get; }
}
