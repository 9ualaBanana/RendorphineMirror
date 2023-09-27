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

public record ExecutedRTask : TypedRegisteredTask
{
    public ExecutedRTask(string Id, TaskAction Action)
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

public record UserRTaskApi : TaskApi
{
    readonly Apis _api;
    public string SessionId { get; }

    public UserRTaskApi(TaskApi rTask, string sessionId)
        : base(rTask)
    {
        _api = Apis.DefaultWithSessionId(sessionId);
        SessionId = sessionId;
    }

    public async ValueTask<ServerTaskState> GetStateAsync()
        => await _api.GetTaskStateAsyncOrThrow(this).ThrowIfError();
}

public static class UserRTaskApiExtensions
{
    public static UserRTaskApi With(this TaskApi rTask, string sessionId)
        => new(rTask, sessionId);
}

public record ExecutedRTaskApi : ExecutedRTask, IRegisteredTaskApi
{
    public ExecutedRTaskApi(string Id = default!, TaskAction Action = default!)
        : base(Id, Action)
    {
    }

    public string? HostShard { get; set; }
}

public record UserExecutedRTask : ExecutedRTaskApi, IRegisteredTaskApi
{
    readonly Apis _api;
    public string SessionId { get; }

    internal UserExecutedRTask(ExecutedRTaskApi executedRTask, string sessionId)
        : this(executedRTask.Id, executedRTask.Action, sessionId)
    {
    }
    public UserExecutedRTask(string id, TaskAction action, string sessionId)
        : base(id, action)
    {
        _api = Apis.DefaultWithSessionId(sessionId);
        SessionId = sessionId;
    }

    public async ValueTask<Uri> GetFileDownloadLinkAsyncUsing(string iid, Extension extension)
        => new(await _api.GetMPlusItemDownloadLinkAsync(this, iid, extension).ThrowIfError());

    public async ValueTask<ServerTaskState> GetStateAsync()
        => await _api.GetTaskStateAsyncOrThrow(this).ThrowIfError();

    public async ValueTask ChangeStateAsyncTo(TaskState state)
        => await _api.ChangeStateAsync(this, state).ThrowIfError();
}

public static class ExecutedRTaskExtensions
{
    public static UserExecutedRTask With(this ExecutedRTaskApi executedRTask, string sessionId)
        => new(executedRTask, sessionId);
}
