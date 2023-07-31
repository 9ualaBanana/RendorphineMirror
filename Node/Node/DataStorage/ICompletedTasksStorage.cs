namespace Node.DataStorage;

public interface ICompletedTasksStorage
{
    DatabaseValueDictionary<string, CompletedTask> CompletedTasks { get; }
}
