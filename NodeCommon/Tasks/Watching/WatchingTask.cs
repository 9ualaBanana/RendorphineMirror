using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NodeCommon.Tasks.Watching;

public class WatchingTask : ILoggable
{
    string ILoggable.LogName => $"WTask {Id}";

    public string Id { get; init; }
    public IWatchingTaskInputInfo Source { get; init; }
    public IWatchingTaskOutputInfo Output { get; init; }
    public string TaskAction { get; init; }
    public JObject TaskData { get; init; }
    public TaskPolicy Policy { get; init; }
    public bool IsPaused = false;
    public ImmutableArray<TaskSoftwareRequirement>? SoftwareRequirements { get; init; }

    [JsonIgnore] public IDisposable? Handler;

    public readonly List<string> PlacedNonCompletedTasks = new();

#pragma warning disable CS8618 // field are not set
    [JsonConstructor] private WatchingTask() { }
#pragma warning restore

    public WatchingTask(string taskaction, JObject taskData, IWatchingTaskInputInfo source, IWatchingTaskOutputInfo output, TaskPolicy policy)
    {
        Id = Guid.NewGuid().ToString();

        Source = source;
        TaskAction = taskaction;
        TaskData = taskData;
        Output = output;
        Policy = policy;
    }


    public string FSDataDirectory() => DirectoryCreated(Path.Combine(Init.ConfigDirectory, "watchingtasks", Id));
    static string DirectoryCreated(string dir)
    {
        Directory.CreateDirectory(dir);
        return dir;
    }
}
