namespace NodeCommon.Tasks;

public interface ITask
{
    public string Id { get; }
}

public record RegisteredTask(string Id) : ITask
{
    public static RegisteredTask With(string id) => new(id);

    /// <summary>
    /// <b>Not intended for use. Required for model binding.</b>.
    /// </summary>
    public RegisteredTask() : this(default(string)!)
    {
    }
}

public record ExecutedTask : RegisteredTask
{
    public string Executor { get; init; } = default!;
    public HashSet<string> UploadedFiles { get; init; } = default!;
}

public interface ITaskApi : ITask
{
    string? HostShard { get; set; }
}

/// <summary>
/// Default implementation of <see cref="ITaskApi"/>.
/// </summary>
public record TaskApi : ITaskApi
{
    public string Id { get; set; }

    public string? HostShard { get; set; }

    public static TaskApi For(ITask task, string? hostShard = default)
        => new(task.Id, hostShard);

    TaskApi(string id, string? hostShard = default)
    {
        Id = id;
        HostShard = hostShard;
    }

    /// <summary>
    /// <b>Not intended for use. Required for model binding.</b>.
    /// </summary>
    public TaskApi()
    {
        Id = default!;
        HostShard = default;
    }
}

public record ExecutedTaskApi : ExecutedTask, ITaskApi
{
    public string? HostShard { get; set; }
}

public record ReceivedTask(string Id, TaskInfo Info) : TaskBase(Id, Info), ILoggable
{
    string ILoggable.LogName => $"Task {Id}";

    public readonly HashSet<FileWithFormat> InputFiles = new();
    public readonly HashSet<FileWithFormat> OutputFiles = new();
    public readonly HashSet<IUploadedFileInfo> UploadedFiles = new();


    public string FSInputFile() => InputFiles.Single().Path;
    public string FSInputFile(FileFormat format) => InputFiles.First(x => x.Format == format).Path;
    public string FSOutputFile(FileFormat format) => OutputFiles.First(x => x.Format == format).Path;
    public string? TryFSOutputFile(FileFormat format) => OutputFiles.FirstOrDefault(x => x.Format == format)?.Path;
}