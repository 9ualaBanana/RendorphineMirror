using Node.Profiling;

namespace Node;

public class NodeSettingsInstance : IPlacedTasksStorage, IQueuedTasksStorage, ICompletedTasksStorage, IWatchingTasksStorage, IRFProductStorage, INodeLoadStorage
{
    public DatabaseValueDictionary<string, ReceivedTask> QueuedTasks { get; }
    public DatabaseValueDictionary<string, WatchingTask> WatchingTasks { get; }
    public DatabaseValueDictionary<string, DbTaskFullState> PlacedTasks { get; }
    public DatabaseValueDictionary<string, CompletedTask> CompletedTasks { get; }
    public DatabaseValueDictionary<string, RFProduct> RFProducts { get; }
    public DatabaseAccessor<long, NodeLoad> NodeFullLoad { get; }
    public Database LoadDatabase { get; }

    public NodeSettingsInstance(DataDirs dirs)
    {
        QueuedTasks = new(new Database(dirs.DataFile("queued.db")), nameof(QueuedTasks), t => t.Id, serializer: JsonSettings.Default);
        WatchingTasks = new(new Database(dirs.DataFile("watching.db")), nameof(WatchingTasks), t => t.Id, serializer: JsonSettings.Default);
        PlacedTasks = new(new Database(dirs.DataFile("placed.db")), nameof(PlacedTasks), t => t.Id, serializer: JsonSettings.Default);
        CompletedTasks = new(new Database(dirs.DataFile("completed.db")), nameof(CompletedTasks), t => t.TaskInfo.Id, serializer: JsonSettings.Default);
        RFProducts = new(new Database(dirs.DataFile("rfproducts.db")), nameof(RFProducts), p => p.ID, serializer: new JsonSerializer { NullValueHandling = NullValueHandling.Ignore });

        LoadDatabase = new Database(dirs.DataFile("load.db"));
        NodeFullLoad = new(LoadDatabase, nameof(NodeFullLoad), "integer");
    }
}
