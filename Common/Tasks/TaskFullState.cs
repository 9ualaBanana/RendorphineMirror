using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Common.Tasks;

public record TaskFullState(TaskState State, double Progress, JObject Output, TaskServer? Server = null);
public record TaskServer(string Host, string Userid, string Nickname);

public record DbTaskFullState : ReceivedTask
{
    public string UserId { get; init; } = null!;
    public ulong Registered { get; init; }
    public TaskServer? Server { get; init; }
    public TaskState State = TaskState.Queued;

    [JsonConstructor]
    public DbTaskFullState(string id, string originGuid, TaskPolicy launchPolicy, TaskObject @object, JObject input, JObject output, JObject data)
        : base(id, new TaskInfo(@object, input, output, data, launchPolicy, originGuid), false) { }
    public DbTaskFullState(string id, string originGuid, TaskPolicy launchPolicy, TaskObject @object, ITaskInputInfo input, ITaskOutputInfo output, JObject data)
        : base(id, new TaskInfo(@object, input, output, data, launchPolicy, originGuid), false) { }
}