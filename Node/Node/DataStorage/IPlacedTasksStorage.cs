namespace Node.DataStorage;

public interface IPlacedTasksStorage
{
    DatabaseValueDictionary<string, DbTaskFullState> PlacedTasks { get; }
}
