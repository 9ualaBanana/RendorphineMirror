namespace NodeCommon.Tasks;

/// <summary>
/// Task that has a unique <see cref="Id"/>.
/// </summary>
public interface IRegisteredTask : IEquatable<IRegisteredTask>
{
    public string Id { get; }
}

public interface IRegisteredTaskApi : IRegisteredTask
{
    string? HostShard { get; set; }
}

/// <summary>
/// <see cref="IRegisteredTask"/> with known <see cref="Action"/>.
/// </summary>
public interface ITypedRegisteredTask : IRegisteredTask
{
    public TaskAction Action { get; }
}

/// <summary>
/// Default implementation of <see cref="IRegisteredTask"/>.
/// </summary>
public record RegisteredTask(string Id) : IRegisteredTask
{
    public static RegisteredTask With(string id) => new(id);

    public bool Equals(IRegisteredTask? other) => Id == other?.Id;
}

/// <summary>
/// Default implementation of <see cref="ITypedRegisteredTask"/>.
/// </summary>
public record TypedRegisteredTask(string Id, TaskAction Action) : ITypedRegisteredTask
{
    public static TypedRegisteredTask With(string id, TaskAction action) => new(id, action);

    public bool Equals(IRegisteredTask? other) => Id == other?.Id;

    public class IdEqualityComparer : IEqualityComparer<TypedRegisteredTask>
    {
        bool IEqualityComparer<TypedRegisteredTask>.Equals(TypedRegisteredTask? x, TypedRegisteredTask? y)
            => x?.Id == y?.Id;

        int IEqualityComparer<TypedRegisteredTask>.GetHashCode(TypedRegisteredTask obj)
            => obj.Id.GetHashCode();
    }
}

public record ExecutedTask : TypedRegisteredTask
{
    public ExecutedTask(string Id, TaskAction Action)
        : base(Id, Action)
    {
    }

    public string Executor { get; init; } = default!;
    public HashSet<string> UploadedFiles { get; init; } = default!;
}

/// <summary>
/// Default implementation of <see cref="IRegisteredTaskApi"/>.
/// </summary>
public record TaskApi(string Id) : IRegisteredTaskApi
{
    public string? HostShard { get; set; }

    public static TaskApi For(IRegisteredTask task)
        => new(task.Id);

    public bool Equals(IRegisteredTask? other) => Id == other?.Id;
}

public record ExecutedTaskApi : ExecutedTask, IRegisteredTaskApi
{
    public ExecutedTaskApi(string Id = default!, TaskAction Action = default!)
        : base(Id, Action)
    {
    }

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