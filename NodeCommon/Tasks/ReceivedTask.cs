namespace NodeCommon.Tasks;

public interface ITask
{
    public string Id { get; }
}

public interface ITaskApi : ITask
{
    string? HostShard { get; set; }
}

public interface ITypedTask : ITask
{
    public string Type { get; }
}

public record RegisteredTask(string Id) : ITask
{
    public static RegisteredTask With(string id) => new(id);
}

public record RegisteredTypedTask(string Id = default!, string Type = default!) : ITypedTask
{
    public static RegisteredTypedTask With(string id, string type) => new(id, type);
}

public record ExecutedTask : RegisteredTypedTask
{
    public string Executor { get; init; } = default!;
    public HashSet<string> UploadedFiles { get; init; } = default!;
}

/// <summary>
/// Default implementation of <see cref="ITaskApi"/>.
/// </summary>
public record TaskApi(string Id = default!) : ITaskApi
{
    public string? HostShard { get; set; }

    public static TaskApi For(ITask task)
        => new(task.Id);
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