using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Common.Tasks.Watching;

public class WatchingTask : ILoggable
{
    string ILoggable.LogName => $"Watching task {Id}";

    public readonly string Id;
    public readonly string? Version;
    public readonly IWatchingTaskSource Source;
    public readonly string TaskAction;
    public readonly JObject TaskData;
    public readonly IWatchingTaskOutputInfo Output;
    public readonly TaskPolicy Policy;
    public readonly bool ExecuteLocally;
    public bool IsPaused = false;

    public readonly List<string> PlacedTasks = new();

#pragma warning disable CS8618 // field are not assigned
    [JsonConstructor] private WatchingTask() { }
#pragma warning restore

    public WatchingTask(IWatchingTaskSource source, string taskaction, JObject taskData, IWatchingTaskOutputInfo output, TaskPolicy policy, string? version, bool executeLocally)
    {
        Id = Guid.NewGuid().ToString();

        Source = source;
        TaskAction = taskaction;
        TaskData = taskData;
        Output = output;
        Policy = policy;
        ExecuteLocally = executeLocally;
    }


    public string FSDataDirectory() => DirectoryCreated(Path.Combine(Init.WatchingTaskFilesDirectory, Id));
    static string DirectoryCreated(string dir)
    {
        Directory.CreateDirectory(dir);
        return dir;
    }
}
