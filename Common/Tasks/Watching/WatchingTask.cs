using Newtonsoft.Json.Linq;

namespace Common.Tasks.Watching;

public class WatchingTask : ILoggable
{
    string ILoggable.LogName => $"Watching task {Id}";

    public string Id { get; }
    public readonly IWatchingTaskSource Source;
    public readonly string TaskAction;
    public readonly JObject TaskData;
    public readonly IWatchingTaskOutputInfo Output;
    public readonly TaskPolicy Policy;

    public WatchingTask(IWatchingTaskSource source, string taskaction, JObject taskData, IWatchingTaskOutputInfo output, TaskPolicy policy = TaskPolicy.SameNode, bool executeLocally = false, string? id = null)
    {
        Id = id ?? Guid.NewGuid().ToString();

        Source = source;
        TaskAction = taskaction;
        TaskData = taskData;
        Output = output;
        Policy = policy;

        if (executeLocally) Policy = TaskPolicy.OwnNodes;
    }


    // TODO: version
    public WatchingTaskInfo AsInfo() => new(Id, null, TaskAction, JObject.FromObject(Source), JObject.FromObject(Output), TaskData, Policy);
}
