namespace NodeCommon.Tasks;

public abstract record TaskBase(string Id, TaskInfo Info) : IMPlusTask
{
    public string? HostShard { get; set; }

    // 0-1
    public double Progress { get; set; } = 0;
    public TaskState State { get; set; } = TaskState.Queued;
    public TaskTimes Times { get; set; } = new();

    [JsonIgnore] public string FirstAction => Info.FirstTaskType;
    [JsonIgnore] public IEnumerable<string> Actions => (Info.Next ?? ImmutableArray<Newtonsoft.Json.Linq.JObject>.Empty).Select(TaskInfo.GetTaskType).Prepend(FirstAction);
    [JsonIgnore] public ITaskInputInfo Input => Info.Input;
    [JsonIgnore] public ITaskOutputInfo Output => Info.Output;

    public void SetStateTime(TaskState state)
    {
        var time = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        Times ??= new();
        Times = state switch
        {
            TaskState.Queued => Times,
            TaskState.Input => Times with { Input = Times.Input ?? time },
            TaskState.Active => Times with { Active = Times.Active ?? time },
            TaskState.Output => Times with { Output = Times.Output ?? time },
            TaskState.Validation => Times with { Output = Times.Validation ?? time },
            TaskState.Finished => Times with { Finished = Times.Finished ?? time },
            TaskState.Failed => Times with { Failed = Times.Failed ?? time },
            TaskState.Canceled => Times with { Canceled = Times.Canceled ?? time },
            _ => throw new InvalidOperationException(),
        };
    }

    public static string GenerateLocalId() => "local_" + Guid.NewGuid();


    public string FSDataDirectory(DataDirs dirs) => FSDataDirectory(dirs, Id);
    public string FSOutputDirectory(DataDirs dirs, string? add = null) => FSOutputDirectory(dirs, Id, add);
    public string FSInputDirectory(DataDirs dirs) => FSInputDirectory(dirs, Id);

    public string FSPlacedDataDirectory(DataDirs dirs) => FSPlacedDataDirectory(dirs, Id);
    public string FSPlacedResultsDirectory(DataDirs dirs) => FSPlacedResultsDirectory(dirs, Id);
    public string FSPlacedSourcesDirectory(DataDirs dirs) => FSPlacedSourcesDirectory(dirs, Id);

    public static string FSTaskDataDirectory(DataDirs dirs) => Directories.DirCreated(dirs.Data, "tasks");
    public static string FSDataDirectory(DataDirs dirs, string id) => Directories.DirCreated(FSTaskDataDirectory(dirs), id);
    public static string FSOutputDirectory(DataDirs dirs, string id, string? add = null) => Directories.DirCreated(FSDataDirectory(dirs, id), "output" + add);
    public static string FSInputDirectory(DataDirs dirs, string id) => Directories.DirCreated(FSDataDirectory(dirs, id), "input");

    public static string FSPlacedTaskDataDirectory(DataDirs dirs) => Directories.DirCreated(dirs.Data, "ptasks");
    public static string FSPlacedDataDirectory(DataDirs dirs, string id) => Directories.DirCreated(FSPlacedTaskDataDirectory(dirs), id);
    public static string FSPlacedResultsDirectory(DataDirs dirs, string id) => Directories.DirCreated(FSPlacedDataDirectory(dirs, id), "results");
    public static string FSPlacedSourcesDirectory(DataDirs dirs, string id) => Directories.DirCreated(FSPlacedDataDirectory(dirs, id), "sources");

    public bool Equals(IRegisteredTask? other) => Id == other?.Id;
}