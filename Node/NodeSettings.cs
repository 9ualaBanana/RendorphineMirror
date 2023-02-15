using Newtonsoft.Json.Linq;
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
        QueuedTasks = new(nameof(QueuedTasks), t => t.Id, serializer: JsonSettings.Default);
        WatchingTasks = new(nameof(WatchingTasks), t => t.Id, serializer: JsonSettings.Default);
        PlacedTasks = new(nameof(PlacedTasks), t => t.Id, serializer: JsonSettings.Default);
        CompletedTasks = new(nameof(CompletedTasks), t => t.TaskInfo.Id, serializer: JsonSettings.Default);
        AcceptTasks = new(nameof(AcceptTasks), true);

        try { BenchmarkResult = new(nameof(BenchmarkResult), default); }
        catch
        {
            new DatabaseValue<object?>(nameof(BenchmarkResult), default).Delete();
            BenchmarkResult = new(nameof(BenchmarkResult), default);
        }
    }


    public readonly record struct BenchmarkInfo(Version Version, BenchmarkData Data);
}