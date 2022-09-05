using Newtonsoft.Json.Linq;

namespace Common.Tasks;

public record TaskFullState(TaskState State, double Progress, JObject Output, TaskServer? Server = null);
public record TaskServer(string Host, string Userid, string Nickname);


public record DbTaskFullState(string Id, string UserId, ulong Registered, string OriginGuid, TaskPolicy LaunchPolicy,
    double Progress, TaskObject @Object, JObject Input, JObject Output, JObject Data, TaskServer? Server = null)
    : TaskInfo(Object, Input, Output, Data, LaunchPolicy, OriginGuid), ITask
{
    string ILoggable.LogName => $"Placed task {Id}";
    bool ITask.ExecuteLocally => false;
    string ITask.Action => TaskType;

    public TaskState State = TaskState.Active;

    ITaskInputInfo ITask.Input => throw new NotImplementedException();
    ITaskOutputInfo ITask.Output => throw new NotImplementedException();

    public ReceivedTask ToReceived() => new ReceivedTask(Id, new TaskInfo(@Object, Input, Output, Data, LaunchPolicy, OriginGuid), false) { Progress = Progress };
}

public class DbTaskFullState1 : ITask
{
    string ILoggable.LogName => $"Placed task {Id}";

    public string Id => Task.Id;
    public bool ExecuteLocally => Task.ExecuteLocally;
    public string Action => Task.Action;

    public ITaskInputInfo Input => Task.Input;
    public ITaskOutputInfo Output => Task.Output;

    public string UserId { get; init; } = null!;
    public ulong Registered { get; init; }
    public TaskServer? Server { get; init; }
    readonly ReceivedTask Task;

    public DbTaskFullState1(string id, string originGuid, TaskPolicy launchPolicy, double progress, TaskObject @object, JObject input, JObject output, JObject data) =>
        Task = new ReceivedTask(id, new TaskInfo(@object, input, output, data, launchPolicy, originGuid), false) { Progress = progress };
}

public record DbTaskFullState2 : ReceivedTask
{
    public string UserId { get; init; } = null!;
    public ulong Registered { get; init; }
    public TaskServer? Server { get; init; }

    public DbTaskFullState2(string id, string originGuid, TaskPolicy launchPolicy, double progress, TaskObject @object, JObject input, JObject output, JObject data)
        : base(id, new TaskInfo(@object, input, output, data, launchPolicy, originGuid), false) { }
}