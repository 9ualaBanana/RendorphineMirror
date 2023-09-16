namespace NodeCommon.Tasks.Watching;

public class WatchingTask
{
    public event Action<DbTaskFullState>? OnCompleted;

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

    public void Complete(DbTaskFullState task) => OnCompleted?.Invoke(task);

    public string FSDataDirectory(DataDirs dirs) => Directories.DirCreated(dirs.Data, "watchingtasks", Id);
}
