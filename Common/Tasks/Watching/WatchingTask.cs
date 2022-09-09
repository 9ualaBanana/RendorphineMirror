using Newtonsoft.Json.Linq;

namespace Common.Tasks.Watching;

public class WatchingTask : ILoggable
{
    string ILoggable.LogName => $"Watching task {Id}";

    public readonly string Id;
    public readonly IWatchingTaskSource Source;
    public readonly string TaskAction;
    public readonly JObject TaskData;
    public readonly IWatchingTaskOutputInfo Output;
    public readonly TaskPolicy Policy;

    public readonly List<string> PlacedTasks = new();

    public WatchingTask(IWatchingTaskSource source, string taskaction, JObject taskData, IWatchingTaskOutputInfo output, TaskPolicy policy = TaskPolicy.SameNode, string? id = null)
    {
        Id = id ?? Guid.NewGuid().ToString();

        Source = source;
        TaskAction = taskaction;
        TaskData = taskData;
        Output = output;
        Policy = policy;
    }


    public string FSDataDirectory() => DirectoryCreated(Path.Combine(Init.WatchingTaskFilesDirectory, Id));
    static string DirectoryCreated(string dir)
    {
        Directory.CreateDirectory(dir);
        return dir;
    }


    // TODO: version
    public WatchingTaskInfo AsInfo() => new(Id, null, TaskAction, JObject.FromObject(Source), JObject.FromObject(Output), TaskData, Policy);
}
