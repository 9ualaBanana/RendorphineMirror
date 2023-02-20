using Node.Profiling;

namespace Node;

public static class NodeSettings
{
    public static readonly DatabaseValueDictionary<string, ReceivedTask> QueuedTasks;
    public static readonly DatabaseValueDictionary<string, WatchingTask> WatchingTasks;
    public static readonly DatabaseValueDictionary<string, DbTaskFullState> PlacedTasks;
    public static readonly DatabaseValueDictionary<string, CompletedTask> CompletedTasks;
    public static readonly DatabaseValue<BenchmarkInfo?> BenchmarkResult;
    public static readonly DatabaseValue<bool> AcceptTasks;

    static NodeSettings()
    {
        var db = Database.Instance;
        QueuedTasks = new(db, nameof(QueuedTasks), t => t.Id, serializer: JsonSettings.Default);
        WatchingTasks = new(db, nameof(WatchingTasks), t => t.Id, serializer: JsonSettings.Default);
        PlacedTasks = new(db, nameof(PlacedTasks), t => t.Id, serializer: JsonSettings.Default);
        CompletedTasks = new(db, nameof(CompletedTasks), t => t.TaskInfo.Id, serializer: JsonSettings.Default);
        AcceptTasks = new(db, nameof(AcceptTasks), true);

        try { BenchmarkResult = new(db, nameof(BenchmarkResult), default); }
        catch
        {
            new DatabaseValue<object?>(db, nameof(BenchmarkResult), default).Delete();
            BenchmarkResult = new(db, nameof(BenchmarkResult), default);
        }
    }


    public readonly record struct BenchmarkInfo(Version Version, BenchmarkData Data);
}