namespace NodeCommon.Tasks.Watching;

public class WatchingTask : ILoggable
{
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    void ILoggable.Log(LogLevel level, string text) => Logger.Log(level, $"[WTask {Id}] {text}");

    public string Id { get; init; }
    public IWatchingTaskInputInfo Source { get; init; }
    public IWatchingTaskOutputInfo Output { get; init; }
    public string TaskAction { get; init; }
    public JObject TaskData { get; init; }
    public TaskPolicy Policy { get; init; }
    public bool IsPaused = false;
    public ImmutableArray<TaskSoftwareRequirement>? SoftwareRequirements { get; init; }

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


    public string FSDataDirectory() => Directories.Created(Path.Combine(Directories.Data, "watchingtasks", Id));
}
