using Newtonsoft.Json.Linq;

namespace Common.Tasks;

public class WatchingTask : ILoggable
{
    string ILoggable.LogName => $"Watching task {Id}";

    public string Id { get; }
    public readonly IWatchingTaskSource Source;
    public readonly string TaskAction;
    public readonly JObject TaskData;
    public readonly IWatchingTaskOutputInfo Output;
    public readonly bool ExecuteLocally;

    public WatchingTask(IWatchingTaskSource source, string taskaction, JObject taskData, IWatchingTaskOutputInfo output, bool executeLocally, string? id = null)
    {
        Source = source;
        TaskAction = taskaction;
        TaskData = taskData;
        Output = output;
        ExecuteLocally = executeLocally;
        Id = id ?? Guid.NewGuid().ToString();
    }
}
