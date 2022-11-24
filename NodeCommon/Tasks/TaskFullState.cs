using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NodeCommon.Tasks;

public record TaskFullState(TaskState State, double Progress, JObject Output, TaskTimes Times, TaskServer? Server = null);
public record TaskServer(string Host, string Userid, string Nickname);

public record DbTaskFullState : ReceivedTask, ILoggable
{
    string ILoggable.LogName => $"PTask {Id}";

    public string UserId { get; init; } = null!;
    public long Registered { get; set; }
    public TaskServer? Server { get; set; }
    public TaskTimes? Times { get; set; }

    [JsonIgnore] public override bool IsFromSameNode => base.IsFromSameNode || (Server?.Userid == Settings.UserId && Server?.Nickname == Settings.NodeName);

    [JsonConstructor]
    public DbTaskFullState(string id, string originGuid, TaskPolicy launchPolicy, TaskObject @object, JObject input, JObject output, JObject data)
        : base(id, new TaskInfo(@object, input, output, data, launchPolicy, originGuid), false) { }
    public DbTaskFullState(string id, string originGuid, TaskPolicy launchPolicy, TaskObject @object, ITaskInputInfo input, ITaskOutputInfo output, JObject data)
        : base(id, new TaskInfo(@object, input, output, data, launchPolicy, originGuid), false) { }
}