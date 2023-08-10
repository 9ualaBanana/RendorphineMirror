namespace Node.DataStorage;

public interface IQueuedTasksStorage
{
    DatabaseValueDictionary<string, ReceivedTask> QueuedTasks { get; }
}
