using Newtonsoft.Json.Linq;
using Node.Profiling;
using static NodeCommon.Settings;

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

        NodeGlobalState.Instance.WatchingTasks.Bind(WatchingTasks.Bindable);
        NodeGlobalState.Instance.PlacedTasks.Bind(PlacedTasks.Bindable);
        NodeGlobalState.Instance.QueuedTasks.Bind(QueuedTasks.Bindable);

        BenchmarkResult.Bindable.SubscribeChanged(() => NodeGlobalState.Instance.BenchmarkResult.Value = BenchmarkResult.Value is null ? null : JObject.FromObject(BenchmarkResult.Value), true);
    }


    public readonly record struct BenchmarkInfo(Version Version, BenchmarkData Data);
}