using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Common.Tasks.Watching;

public class WatchingTask : ILoggable
{
    string ILoggable.LogName => $"Watching task {Id}";

    public string Id { get; init; }
    public string? Version { get; init; }
    public IWatchingTaskInputInfo Source { get; init; }
    public IWatchingTaskOutputInfo Output { get; init; }
    public string TaskAction { get; init; }
    public JObject TaskData { get; init; }
    public TaskPolicy Policy { get; init; }
    public bool ExecuteLocally { get; init; }
    public bool IsPaused = false;

    [JsonIgnore] public IDisposable? Handler;

    public readonly List<string> PlacedNonCompletedTasks = new();

#pragma warning disable CS8618 // field are not set
    [JsonConstructor] private WatchingTask() { }
#pragma warning restore

    public WatchingTask(string taskaction, JObject taskData, IWatchingTaskInputInfo source, IWatchingTaskOutputInfo output, TaskPolicy policy, string? version, bool executeLocally)
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
