using Newtonsoft.Json;

namespace NodeCommon.Tasks;

public abstract record TaskBase(string Id, TaskInfo Info) : IRegisteredTaskApi, ILoggable
{
    string ILoggable.LogName => $"{GetType().Name} {Id}";

    public string? HostShard { get; set; }

    // 0-1
    public double Progress = 0;
    public TaskState State { get; set; } = TaskState.Queued;
    public TaskTimes Times { get; set; } = new();

    [JsonIgnore] public string Action => Info.TaskType;
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


    public string FSDataDirectory() => FSDataDirectory(Id);
    public string FSOutputDirectory() => FSOutputDirectory(Id);
    public string FSInputDirectory() => FSInputDirectory(Id);

    public string FSPlacedDataDirectory() => FSPlacedDataDirectory(Id);
    public string FSPlacedResultsDirectory() => FSPlacedResultsDirectory(Id);
    public string FSPlacedSourcesDirectory() => FSPlacedSourcesDirectory(Id);

    public static string FSTaskDataDirectory() => DirectoryCreated(Path.Combine(Init.ConfigDirectory, "tasks"));
    public static string FSDataDirectory(string id) => DirectoryCreated(Path.Combine(FSTaskDataDirectory(), id));
    public static string FSOutputDirectory(string id) => DirectoryCreated(Path.Combine(FSDataDirectory(id), "output"));
    public static string FSInputDirectory(string id) => DirectoryCreated(Path.Combine(FSDataDirectory(id), "input"));

    public static string FSPlacedTaskDataDirectory() => DirectoryCreated(Path.Combine(Init.ConfigDirectory, "ptasks"));
    public static string FSPlacedDataDirectory(string id) => DirectoryCreated(Path.Combine(FSPlacedTaskDataDirectory(), id));
    public static string FSPlacedResultsDirectory(string id) => DirectoryCreated(Path.Combine(FSPlacedDataDirectory(id), "results"));
    public static string FSPlacedSourcesDirectory(string id) => DirectoryCreated(Path.Combine(FSPlacedDataDirectory(id), "sources"));


    public string GetTempFileName(string extension)
    {
        if (!extension.StartsWith('.')) extension = "." + extension;

        var tempdir = Init.TempDirectory(Id);
        while (true)
        {
            var file = Path.Combine(tempdir, Guid.NewGuid().ToString() + extension);
            if (File.Exists(file)) continue;

            return file;
        }
    }


    protected static string DirectoryCreated(string dir)
    {
        Directory.CreateDirectory(dir);
        return dir;
    }

    public bool Equals(IRegisteredTask? other) => Id == other?.Id;
}