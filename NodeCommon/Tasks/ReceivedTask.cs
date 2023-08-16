using System.Diagnostics.CodeAnalysis;

namespace NodeCommon.Tasks;

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

    public class IdEqualityComparer : IEqualityComparer<IRegisteredTask>
    {
        public bool Equals(IRegisteredTask? x, IRegisteredTask? y)
            => x?.Id == y?.Id;

        public int GetHashCode([DisallowNull] IRegisteredTask obj)
            => obj.Id.GetHashCode();
    }
}

/// <summary>
/// Default implementation of <see cref="ITypedRegisteredTask"/>.
/// </summary>
public record TypedRegisteredTask(string Id, TaskAction Action) : ITypedRegisteredTask
{
    public static TypedRegisteredTask With(string id, TaskAction action) => new(id, action);
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